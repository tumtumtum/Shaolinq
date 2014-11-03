// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.TypeBuilding
{
	internal sealed class DataAccessModelAssemblyBuilder
		: DataAccessAssemblyProvider
	{
		public override AssemblyBuildInfo GetDataAccessModelAssembly(Type dataAccessModelType, DataAccessModelConfiguration configuration)
		{
			var originalAssembly = dataAccessModelType.Assembly;
			var builtAssembly = BuildAssembly(originalAssembly, configuration);

			return new AssemblyBuildInfo(dataAccessModelType, builtAssembly, originalAssembly, configuration);
		}

		private static Assembly BuildAssembly(Assembly assembly, DataAccessModelConfiguration configuration)
		{
			DataAccessObjectTypeBuilder dataAccessObjectTypeBuilder;

			var configMd5 = configuration.GetMd5();
			var assemblyName = new AssemblyName(assembly.GetName().Name + ".Concrete");
			var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + "." + configMd5 + ".dll");
			
			var assemblyBuildContext = new AssemblyBuildContext
			{
				SourceAssembly = assembly,
				TargetAssembly = assemblyBuilder
			};

			var builder = new DataAccessModelTypeBuilder(assemblyBuildContext, moduleBuilder);
			
			foreach (var type in assembly.GetTypes().Where(typeof(DataAccessModel).IsAssignableFrom))
			{
				builder.BuildType(type);
			}

			var typeProvider = TypeDescriptorProvider.GetProvider(assembly);
			var typeDescriptors = typeProvider.GetTypeDescriptors();
            
			foreach (var typeDescriptor in typeDescriptors)
			{
				dataAccessObjectTypeBuilder = new DataAccessObjectTypeBuilder(typeProvider, assemblyBuildContext, moduleBuilder, typeDescriptor.Type);

				dataAccessObjectTypeBuilder.BuildFirstPhase(1);
			}

			foreach (var typeDescriptor in typeDescriptors)
			{
				dataAccessObjectTypeBuilder = assemblyBuildContext.TypeBuilders[typeDescriptor.Type];

				dataAccessObjectTypeBuilder.BuildFirstPhase(2);
			}

			assemblyBuildContext.Dispose();

			bool saveConcreteAssembly;
			bool.TryParse(ConfigurationManager.AppSettings["Shaolinq.SaveConcreteAssembly"], out saveConcreteAssembly);

#if DEBUG
			const bool isInDebugMode = true;
#else
			const bool isInDebugMode = false;
#endif

			if (saveConcreteAssembly || isInDebugMode)
			{
				ActionUtils.IgnoreExceptions(() => assemblyBuilder.Save(assemblyName + "." + configMd5 + ".dll"));
			}

			return assemblyBuilder;
		}
	}
}
