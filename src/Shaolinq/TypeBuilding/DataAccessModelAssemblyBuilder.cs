// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using Platform;
using Platform.IO;
using Platform.Text;
using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq.TypeBuilding
{
	public class DataAccessModelAssemblyBuilder
		: DataAccessAssemblyProvider
	{
		public override RuntimeDataAccessModelInfo GetDataAccessModelAssembly(Type dataAccessModelType, DataAccessModelConfiguration configuration)
		{
			var typeDescriptorProvider = new TypeDescriptorProvider(dataAccessModelType, configuration);
			var originalAssembly = dataAccessModelType.Assembly;
			var builtAssembly = BuildAssembly(typeDescriptorProvider, configuration);

			return new RuntimeDataAccessModelInfo(typeDescriptorProvider, builtAssembly, originalAssembly);
		}

		private static string ReadResource(Assembly assembly, string resourceName)
		{
			using (var stream = assembly.GetManifestResourceStream(resourceName))
			{
				if (stream == null)
				{
					return null;
				}

				using (var reader = new StreamReader(stream, Encoding.UTF8))
				{
					return reader.ReadToEnd();
				}
			}
		}

		private static Assembly BuildAssembly(TypeDescriptorProvider typeDescriptorProvider, DataAccessModelConfiguration configuration)
		{
			string fullhash;
			DataAccessObjectTypeBuilder dataAccessObjectTypeBuilder;
			var serializedConfiguration = XmlSerializer<DataAccessModelConfiguration>.New().SerializeToString(configuration);
			
			var filename = GetFileName(typeDescriptorProvider, configuration, serializedConfiguration, out fullhash);

			if (configuration.SaveAndReuseGeneratedAssemblies ?? false)
			{
				if (filename != null && File.Exists(filename))
				{
					var candidate = Assembly.LoadFile(filename);

					if (ReadResource(candidate, "configuration.xml") == serializedConfiguration
						&& ReadResource(candidate, "sha1.txt") == fullhash)
					{
						return candidate;
					}
				}
			}

			var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
			var typeDescriptors = typeDescriptorProvider.GetTypeDescriptors();
			var assemblyName = new AssemblyName(filenameWithoutExtension);
			var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave, Path.GetDirectoryName(filename));
			var moduleBuilder = assemblyBuilder.DefineDynamicModule(filenameWithoutExtension, filenameWithoutExtension + ".dll");

			var propertiesBuilder = moduleBuilder.DefineType("$$$DataAccessModelProperties", TypeAttributes.Class, typeof(object));
			var assemblyBuildContext = new AssemblyBuildContext(assemblyBuilder, moduleBuilder, propertiesBuilder);

			var dataAccessModelTypeBuilder = new DataAccessModelTypeBuilder(assemblyBuildContext, moduleBuilder);
			dataAccessModelTypeBuilder.BuildTypePhase1(typeDescriptorProvider.DataAccessModelType);
			
			foreach (var typeDescriptor in typeDescriptors)
			{
				dataAccessObjectTypeBuilder = new DataAccessObjectTypeBuilder(typeDescriptorProvider, assemblyBuildContext, moduleBuilder, typeDescriptor.Type);
				dataAccessObjectTypeBuilder.Build(new DataAccessObjectTypeBuilder.TypeBuildContext(1));
			}

			foreach (var typeDescriptor in typeDescriptors)
			{
				dataAccessObjectTypeBuilder = assemblyBuildContext.TypeBuilders[typeDescriptor.Type];
				dataAccessObjectTypeBuilder.Build(new DataAccessObjectTypeBuilder.TypeBuildContext(2));
			}

			assemblyBuildContext.DataAccessModelPropertiesTypeBuilder.CreateType();
			assemblyBuildContext.FinishConstantsContainer();
			assemblyBuildContext.ConstantsContainer.CreateType();
			
			dataAccessModelTypeBuilder.BuildTypePhase2();
			
#if DEBUG
			const bool isInDebugMode = true;
#else
			const bool isInDebugMode = false;
#endif

			var saveAssembly = configuration.SaveAndReuseGeneratedAssemblies ?? !isInDebugMode;

			if (saveAssembly)
			{
				ActionUtils.IgnoreExceptions(() =>
				{
					moduleBuilder.DefineManifestResource("configuration.xml", new MemoryStream(Encoding.UTF8.GetBytes(serializedConfiguration)), ResourceAttributes.Public);
					moduleBuilder.DefineManifestResource("sha1.txt", new MemoryStream(Encoding.UTF8.GetBytes(fullhash)), ResourceAttributes.Public);

					assemblyBuilder.Save(Path.GetFileName(filename));
				});
			}

			return assemblyBuilder;
		}

		private static string GetFileName(TypeDescriptorProvider typeDescriptorProvider, DataAccessModelConfiguration configuration, string serializedConfiguration, out string fullhash)
		{
			var sha1 = SHA1.Create();
			var modelAssembly = typeDescriptorProvider.DataAccessModelType.Assembly;
			var uniquelyReferencedAssemblies = new HashSet<Assembly> { typeof(DataAccessModel).Assembly, modelAssembly };

			foreach (var type in typeDescriptorProvider.GetTypeDescriptors())
			{
				var current = type.Type;

				while (current != null)
				{
					uniquelyReferencedAssemblies.Add(current.Assembly);

					current = current.BaseType;
				}
			}

			var bytes = Encoding.UTF8.GetBytes(serializedConfiguration);

			sha1.TransformBlock(bytes, 0, bytes.Length, null, 0);

			foreach (var assembly in uniquelyReferencedAssemblies.OrderBy(c => c.FullName))
			{
				bytes = Encoding.UTF8.GetBytes(assembly.FullName);

				sha1.TransformBlock(bytes, 0, bytes.Length, null, 0);
				
				var path = StringUriUtils.GetScheme(assembly.CodeBase) == "file" ? new Uri(assembly.CodeBase).LocalPath : assembly.Location;

				if (path != null)
				{
					if (assembly.GetName().GetPublicKeyToken().Length == 0)
					{
						var fileInfo = new FileInfo(path);
						
						bytes = BitConverter.GetBytes(fileInfo.Length);
						sha1.TransformBlock(bytes, 0, bytes.Length, null, 0);

						bytes = BitConverter.GetBytes(fileInfo.LastWriteTimeUtc.Ticks);
						sha1.TransformBlock(bytes, 0, bytes.Length, null, 0);
					}
				}
			}

			sha1.TransformFinalBlock(bytes, 0, 0);

			var fileName = modelAssembly.Location == null ? modelAssembly.GetName().Name : Path.GetFileNameWithoutExtension(modelAssembly.Location);
			var cacheDirectory = configuration.GeneratedAssembliesSaveDirectory?.Trim();
			var codebaseUri = new Uri(modelAssembly.CodeBase);

			var modelName = typeDescriptorProvider.DataAccessModelType.Name;

			if (modelAssembly.GetExportedTypes().Any(c => c.Name == modelName && c != typeDescriptorProvider.DataAccessModelType))
			{
				modelName = typeDescriptorProvider.DataAccessModelType.FullName.Replace(".", "_");
			}

			fullhash = TextConversion.ToHexString(sha1.Hash);

			fileName = $"{fileName}.{modelName}.Generated.dll";
			cacheDirectory = !string.IsNullOrEmpty(cacheDirectory) ? cacheDirectory : !codebaseUri.IsFile ? Environment.CurrentDirectory : Path.GetDirectoryName(codebaseUri.LocalPath);
			
			return Path.Combine(cacheDirectory, fileName);
		}
	}
}
