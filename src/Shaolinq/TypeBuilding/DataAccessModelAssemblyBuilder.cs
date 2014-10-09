// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

 using System;
using System.Collections.Generic;
 using System.Linq;
 using System.Reflection;
using System.Reflection.Emit;
using Shaolinq.Persistence;
using Platform;

namespace Shaolinq.TypeBuilding
{
	public class DataAccessModelAssemblyBuilder
		: MarshalByRefObject
	{
		private class ObjectEqualityComparer<T>
            : IEqualityComparer<T>
		{
			public static readonly ObjectEqualityComparer<T> Default = new ObjectEqualityComparer<T>();

			public bool Equals(T x, T y)
			{
				return x.Equals(y);
			}

			public int GetHashCode(T obj)
			{
				return this.GetHashCode();
			}
		}

		private readonly Dictionary<Assembly, AssemblyBuildInfo> builtAssemblies = new Dictionary<Assembly, AssemblyBuildInfo>();
		private Dictionary<Assembly, Assembly> definitionByConcrete = new Dictionary<Assembly, Assembly>(ObjectEqualityComparer<Assembly>.Default);
		public static DataAccessModelAssemblyBuilder Default = new DataAccessModelAssemblyBuilder();
		
		public Assembly GetDefinitionAssembly(Assembly concreteAssembly)
		{
			Assembly retval;

			if (definitionByConcrete.TryGetValue(concreteAssembly, out retval))
			{
				return retval;
			}

			return concreteAssembly;
		}

		public AssemblyBuildInfo GetAssemblyBuildInfo(Assembly assembly)
		{
			lock (typeof(TypeDescriptorProvider))
			{
				AssemblyBuildInfo retval;

				assembly = GetDefinitionAssembly(assembly);

				if (builtAssemblies.TryGetValue(assembly, out retval))
				{
					return retval;
				}
			}

			throw new InvalidOperationException();
		}

		public AssemblyBuildInfo GetOrBuildConcreteAssembly(Assembly definitionAssembly)
		{
			AssemblyBuildInfo value;

			lock (typeof(TypeDescriptorProvider))
			{
				definitionAssembly = GetDefinitionAssembly(definitionAssembly);

				if (!builtAssemblies.TryGetValue(definitionAssembly, out value))
				{
					value = new AssemblyBuildInfo(BuildAssembly(definitionAssembly), definitionAssembly);

					builtAssemblies[definitionAssembly] = value;

					var newDefinitionByConcrete = new Dictionary<Assembly, Assembly>(ObjectEqualityComparer<Assembly>.Default);

					foreach (var kvp in definitionByConcrete)
					{
						newDefinitionByConcrete[kvp.Key] = kvp.Value;
					}

					newDefinitionByConcrete[value.concreteAssembly] = definitionAssembly;

					definitionByConcrete = newDefinitionByConcrete;
				}
			}

			return value;
		}

		protected virtual Assembly BuildAssembly(Assembly assembly)
		{
			DataAccessObjectTypeBuilder dataAccessObjectTypeBuilder;
            
			var assemblyName = new AssemblyName(assembly.GetName().Name + ".Concrete");
			var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");
			
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

#if DEBUG
			try
			{
				assemblyBuilder.Save(assemblyName + ".dll");
			}
			catch
			{
			}
#endif
			
			return assemblyBuilder;
		}
	}
}
