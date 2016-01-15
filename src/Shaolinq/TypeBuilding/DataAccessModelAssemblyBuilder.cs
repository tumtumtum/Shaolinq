// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

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
			var typeDescriptorProvider = new TypeDescriptorProvider(dataAccessModelType, configuration);
			var originalAssembly = dataAccessModelType.Assembly;
			var builtAssembly = BuildAssembly(typeDescriptorProvider, configuration);

			return new RuntimeDataAccessModelInfo(typeDescriptorProvider, builtAssembly, originalAssembly);
		}

		private static Assembly BuildAssembly(TypeDescriptorProvider typeDescriptorProvider, DataAccessModelConfiguration configuration)
		{
			DataAccessObjectTypeBuilder dataAccessObjectTypeBuilder;

			var configSha256 = configuration.GetSha256();
			var assemblyName = new AssemblyName(typeDescriptorProvider.DataAccessModelType.Assembly.GetName().Name + "." + typeDescriptorProvider.DataAccessModelType.Name);
			var sharedAssemblyName = new AssemblyName("Shaolinq.GeneratedDataAccessModel");
			var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(sharedAssemblyName, AssemblyBuilderAccess.RunAndSave);
			var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + "." + configSha256 + ".dll");

			var assemblyBuildContext = new AssemblyBuildContext(assemblyBuilder);

			var dataAccessModelTypeBuilder = new DataAccessModelTypeBuilder(assemblyBuildContext, moduleBuilder);
			dataAccessModelTypeBuilder.BuildType(typeDescriptorProvider.DataAccessModelType);

			var typeDescriptors = typeDescriptorProvider.GetTypeDescriptors();
            
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

			bool saveConcreteAssembly;
			bool.TryParse(ConfigurationManager.AppSettings["Shaolinq.SaveConcreteAssembly"], out saveConcreteAssembly);

#if DEBUG
			const bool isInDebugMode = true;
#else
			const bool isInDebugMode = false;
#endif

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (saveConcreteAssembly || isInDebugMode)
			{
				ActionUtils.IgnoreExceptions(() => assemblyBuilder.Save(assemblyName + "." + configSha256 + ".dll"));
			}

			return assemblyBuilder;
		}
	}
}
