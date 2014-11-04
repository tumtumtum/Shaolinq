// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Configuration;
using System.Reflection;
using System.Reflection.Emit;
using Platform;
using Shaolinq.Persistence;

namespace Shaolinq.TypeBuilding
{
	public class DataAccessModelAssemblyBuilder
		: DataAccessAssemblyProvider
	{
		public override RuntimeDataAccessModelInfo GetDataAccessModelAssembly(Type dataAccessModelType, DataAccessModelConfiguration configuration)
		{
			var typeDescriptorProvider = new TypeDescriptorProvider(dataAccessModelType);
			var originalAssembly = dataAccessModelType.Assembly;
			var builtAssembly = BuildAssembly(typeDescriptorProvider, configuration);

			return new RuntimeDataAccessModelInfo(typeDescriptorProvider, builtAssembly, originalAssembly);
		}

		private static Assembly BuildAssembly(TypeDescriptorProvider typeDescriptorProvider, DataAccessModelConfiguration configuration)
		{
			DataAccessObjectTypeBuilder dataAccessObjectTypeBuilder;

			var configMd5 = configuration.GetMd5();
			var assemblyName = new AssemblyName(typeDescriptorProvider.DataAccessModelType.Assembly.GetName().Name + ".Concrete");
			var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + "." + configMd5 + ".dll");
			
			var assemblyBuildContext = new AssemblyBuildContext
			{
				SourceAssembly = typeDescriptorProvider.DataAccessModelType.Assembly,
				TargetAssembly = assemblyBuilder
			};

			var dataAccessModelTypeBuilder = new DataAccessModelTypeBuilder(assemblyBuildContext, moduleBuilder);

			dataAccessModelTypeBuilder.BuildType(typeDescriptorProvider.DataAccessModelType);

			var typeDescriptors = typeDescriptorProvider.GetTypeDescriptors();
            
			foreach (var typeDescriptor in typeDescriptors)
			{
				dataAccessObjectTypeBuilder = new DataAccessObjectTypeBuilder(typeDescriptorProvider, assemblyBuildContext, moduleBuilder, typeDescriptor.Type);

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
