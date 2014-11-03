using System;
using System.Collections.Generic;

namespace Shaolinq.TypeBuilding
{
	public class CachingDataAccessModelAssemblyProvider
		: DataAccessAssemblyProvider
	{
		public static readonly CachingDataAccessModelAssemblyProvider Default = new CachingDataAccessModelAssemblyProvider(new DataAccessModelAssemblyBuilder());

		private struct AssemblyKey
		{
			private readonly string configurationMd5;
			private readonly Type dataAccessModelType;

			private AssemblyKey(Type dataAccessModelType, string configurationMd5)
				: this()
			{
				this.dataAccessModelType = dataAccessModelType;
				this.configurationMd5 = configurationMd5;
			}

			public AssemblyKey(Type dataAccessModelType, DataAccessModelConfiguration configuration)
				: this(dataAccessModelType, configuration.GetMd5())
			{
			}

			public override int GetHashCode()
			{
				return this.configurationMd5.GetHashCode() ^ this.dataAccessModelType.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				var other = obj as AssemblyKey?;

				if (other == null)
				{
					return false;
				}

				return other.Value.configurationMd5 == this.configurationMd5
				       && other.Value.dataAccessModelType == this.dataAccessModelType;
			}
		}

		private readonly DataAccessAssemblyProvider provider;
		private readonly Dictionary<AssemblyKey, AssemblyBuildInfo> assemblyBuildInfosByKey = new Dictionary<AssemblyKey, AssemblyBuildInfo>();

		public CachingDataAccessModelAssemblyProvider(DataAccessAssemblyProvider provider)
		{
			this.provider = provider;
		}

		public override AssemblyBuildInfo GetDataAccessModelAssembly(Type dataAccessModelType, DataAccessModelConfiguration configuration)
		{
			var key = new AssemblyKey(dataAccessModelType, configuration);

			AssemblyBuildInfo assemblyBuildInfo;

			lock (this.assemblyBuildInfosByKey)
			{
				if (!this.assemblyBuildInfosByKey.TryGetValue(key, out assemblyBuildInfo))
				{
					assemblyBuildInfo = this.provider.GetDataAccessModelAssembly(dataAccessModelType, configuration);

					this.assemblyBuildInfosByKey[key] = assemblyBuildInfo;
				}
			}

			return assemblyBuildInfo;
		}
	}
}