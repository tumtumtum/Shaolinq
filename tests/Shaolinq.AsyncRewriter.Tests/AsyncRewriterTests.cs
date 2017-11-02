// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Shaolinq.AsyncRewriter.Tests
{
	[TestFixture(Category = "IgnoreOnMono")]
	public class AsyncRewriterTests
	{
		private static IEnumerable<TestCaseData> GetTestCases()
		{
			yield return GetTestCaseData("Rewrite", "Foo.cs", "Bar.cs");
			yield return GetTestCaseData("Generic Constraints", "IQuery.cs");
			yield return GetTestCaseData("Interfaces", "ICommand.cs");
			yield return GetTestCaseData("Extension methods", "ExtensionMethodTests.cs", "Foo.cs");
			yield return GetTestCaseData("Conditional access", "ConditionalAccess.cs");
			yield return GetTestCaseData("Assignment", "TestAssignment.cs");
			yield return GetTestCaseData("Generic specialised implementation", "TestGenericSpecialisedImplementation.cs");
			yield return GetTestCaseData("Explicit await rewritten async method", "TestExplicitAwaitRewrittenAsyncMethod.cs");
			yield return GetTestCaseData("Explicit interface implementations", "TestExplicitInterfaceImplementations.cs");
			yield return GetTestCaseData("Expression body", "TestExpressionBody.cs");
			yield return GetTestCaseData("Attribute on class", "TestAttributeOnClass.cs");
			yield return GetTestCaseData("Language features", "LanguageFeatures.cs");
			yield return GetTestCaseData("Static Generic Method Call", "StaticGenericMethodCall.cs");
		}

		private static TestCaseData GetTestCaseData(string name, params string[] inputFiles)
		{
			return new TestCaseData(new object[] {inputFiles}).SetName(name);
		}

		[TestCaseSource(nameof(GetTestCases))]
		public void TestRewrite(params string[] inputFiles)
		{
			var rewriter = new Rewriter();
			var root = Path.Combine(Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath), "RewriteTests");

			var result = rewriter.RewriteAndMerge(inputFiles.Select(c => Path.Combine(root, c)).ToArray());

			Console.WriteLine(result);
		}

		[Explicit]
		[TestCaseSource(nameof(GetTestCases))]
		public void TestRewriteCompile(params string[] inputFiles)
		{
			var rewriter = new Rewriter();
			var root = Path.Combine(Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath), "RewriteTests");
			var files = new List<string>(inputFiles) {"RewriteAsyncAttribute.cs"};

			var rewriteResult = rewriter.RewriteAndMerge(files.Select(c => Path.Combine(root, c)).ToArray());

			var syntaxTrees =
				new List<SyntaxTree>(files.Select(c => CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, c)))))
				{
					CSharpSyntaxTree.ParseText(rewriteResult)
				};

			var assemblyName = Path.GetRandomFileName();
			var references = new MetadataReference[]
			{
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(DataAccessObject).Assembly.Location)
			};

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
			}
		}
	}
}
