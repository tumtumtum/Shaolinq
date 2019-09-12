// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Platform;

namespace Shaolinq.AsyncRewriter.Tests
{
	[TestFixture]
	public class AsyncRewriterTests
	{
		private static IEnumerable<TestCaseData> GetTestCases()
		{
			yield return GetTestCaseData("AmbiguousNamespace", "AmbiguousNamespace.cs", "AmbiguousNamespaceClasses.cs");
			yield return GetTestCaseData("AmbiguousReference", "AmbiguousReference.cs");
			yield return GetTestCaseData("Rewrite", "Foo.cs", "Bar.cs");
			yield return GetTestCaseData("Generic Constraints", "IQuery.cs");
			yield return GetTestCaseData("Interfaces", "ICommand.cs");
			yield return GetTestCaseData("Extension methods", "ExtensionMethodTests.cs", "Foo.cs");
			yield return GetTestCaseData("Extension methods2", "ExtensionMethodTests2.cs");
			yield return GetTestCaseData("Conditional access", "ConditionalAccess.cs");
			yield return GetTestCaseData("Assignment", "TestAssignment.cs");
			yield return GetTestCaseData("Generic specialised implementation", "TestGenericSpecialisedImplementation.cs");
			yield return GetTestCaseData("Explicit await rewritten async method", "TestExplicitAwaitRewrittenAsyncMethod.cs");
			yield return GetTestCaseData("Explicit interface implementations", "TestExplicitInterfaceImplementations.cs");
			yield return GetTestCaseData("Expression body", "TestExpressionBody.cs");
			yield return GetTestCaseData("Attribute on class", "TestAttributeOnClass.cs");
			yield return GetTestCaseData("Language features", "LanguageFeatures.cs");
			yield return GetTestCaseData("Static Generic Method Call", "StaticGenericMethodCall.cs");
			yield return GetTestCaseData("Nested async methods", "NestedAsync.cs");
			yield return GetTestCaseData("Method resolution test", "MethodResolutionTest.cs");
			yield return GetTestCaseData("Generic method call test", "GenericMethods.cs");
		}

		private static TestCaseData GetTestCaseData(string name, params string[] inputFiles)
		{
			return new TestCaseData(new object[] {inputFiles}).SetName(name);
		}

		[TestCaseSource(nameof(GetTestCases))]
		public void TestRewrite(params string[] inputFiles)
		{
			var rewriter = new Rewriter();
			var root = Path.Combine(Path.GetDirectoryName(new Uri(GetType().Assembly.CodeBase).LocalPath), "RewriteTests");

			var result = rewriter.RewriteAndMerge(inputFiles.Select(c => Path.Combine(root, c)).ToArray());
			
			Assert.IsFalse(result.Contains(": N}\""));

			Console.WriteLine(result);
		}

		[TestCaseSource(nameof(GetTestCases))]
		public void TestRewriteCompile(params string[] inputFiles)
		{
			var rewriter = new Rewriter();
			var root = Path.Combine(Path.GetDirectoryName(new Uri(GetType().Assembly.CodeBase).LocalPath), "RewriteTests");
			var files = new List<string>(inputFiles) {"RewriteAsyncAttribute.cs"};
			var references = new MetadataReference[]
			{
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(QueryableExtensions).Assembly.Location)
			};

			var rewriteResult = rewriter.RewriteAndMerge(files.Select(c => Path.Combine(root, c)).ToArray(), references.Select(c => c.Display).ToArray());

			Console.WriteLine(rewriteResult);

			var syntaxTrees =
				new List<SyntaxTree>(files.Select(c => CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, c)), new CSharpParseOptions(LanguageVersion.CSharp7))))
				{
					CSharpSyntaxTree.ParseText(rewriteResult)
				};

			var assemblyName = Path.GetRandomFileName();
			
			var compilation = CSharpCompilation.Create(
				assemblyName,
				syntaxTrees: syntaxTrees,
				references: references,
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

			using (var ms = new MemoryStream())
			{
				var compileResult = compilation.Emit(ms);

				if (!compileResult.Success)
				{
					var failures = compileResult.Diagnostics.Where(diagnostic =>
						diagnostic.IsWarningAsError ||
						diagnostic.Severity == DiagnosticSeverity.Error);

					foreach (var diagnostic in failures)
					{
						Console.Error.WriteLine(diagnostic);
					}

					Assert.Fail();
				}

				var assembly = Assembly.Load(ms.ToArray());

				var asyncMethod = assembly.DefinedTypes
					.SelectMany(c => c.GetMethods())
					.FirstOrDefault(c => c.Name == "MainAsync" && c.IsStatic && c.GetParameters().Length == 0 && c.ReturnType.IsAssignableFromIgnoreGenericParameters(typeof(Task<>)));

				if (asyncMethod != null)
				{
					var task = (Task<int>)asyncMethod.Invoke(null, null);

					var result = task.GetAwaiter().GetResult();

					if (result != 0)
					{
						Assert.Fail();
					}
				}
				else
				{
					var method = assembly.DefinedTypes
						.SelectMany(c => c.GetMethods())
						.FirstOrDefault(c => c.Name == "Main" && c.IsStatic && c.GetParameters().Length == 0);

					if (method != null)
					{
						if (method.ReturnType == typeof(int))
						{
							var result = (int)method.Invoke(null, null);

							if (result != 0)
							{
								Assert.Fail();
							}
						}
						else
						{
							method.Invoke(null, null);
						}
					}
				}

			}
		}
	}
}
