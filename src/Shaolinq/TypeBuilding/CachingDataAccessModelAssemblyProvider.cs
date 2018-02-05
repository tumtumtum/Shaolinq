// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Platform.Text;
using Platform.Xml.Serialization;

namespace Shaolinq.TypeBuilding
{
	public class CachingDataAccessModelAssemblyProvider
		: DataAccessAssemblyProvider
	{
		public static readonly CachingDataAccessModelAssemblyProvider Default = new CachingDataAccessModelAssemblyProvider(new DataAccessModelAssemblyBuilder());

		private struct AssemblyKey
		{
			private readonly string configurationXml;
			private readonly string configurationHash;
			private readonly Type dataAccessModelType;
			
			public AssemblyKey(Type dataAccessModelType, DataAccessModelConfigurationUniqueKey configurationUniqueKey)
			{
				this.dataAccessModelType = dataAccessModelType;
				this.configurationXml = XmlSerializer<DataAccessModelConfigurationUniqueKey>.New().SerializeToString(configurationUniqueKey);
				this.configurationHash = TextConversion.ToHexString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(this.configurationXml)));
			}

			public override int GetHashCode()
			{
				return this.configurationHash.GetHashCode() ^ this.dataAccessModelType.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				if (!(obj is AssemblyKey other))
				{
					return false;
				}
				
				return other.configurationHash == this.configurationHash
						&& other.dataAccessModelType == this.dataAccessModelType
						&& this.configurationXml == other.configurationXml;
			}
		}

		private readonly DataAccessAssemblyProvider provider;
		private readonly HashSet<AssemblyKey> buildingSet = new HashSet<AssemblyKey>();
		private readonly Dictionary<AssemblyKey, RuntimeDataAccessModelInfo> assemblyBuildInfosByKey = new Dictionary<AssemblyKey, RuntimeDataAccessModelInfo>();

		public CachingDataAccessModelAssemblyProvider(DataAccessAssemblyProvider provider)
		{
			this.provider = provider;
		}

		public override RuntimeDataAccessModelInfo GetDataAccessModelAssembly(Type dataAccessModelType, DataAccessModelConfiguration configuration)
		{
			var key = new AssemblyKey(dataAccessModelType, new DataAccessModelConfigurationUniqueKey(configuration));

			lock (this.buildingSet)
			{
				while (true)
				{
					if (this.assemblyBuildInfosByKey.TryGetValue(key, out var runtimeDataAccessModelInfo))
					{
						return runtimeDataAccessModelInfo;
					}

					if (this.buildingSet.Contains(key))
					{
						Monitor.Wait(this.buildingSet);

						continue;
					}

					try
					{
						this.buildingSet.Add(key);

						runtimeDataAccessModelInfo = this.provider.GetDataAccessModelAssembly(dataAccessModelType, configuration);

						this.assemblyBuildInfosByKey[key] = runtimeDataAccessModelInfo;

						return runtimeDataAccessModelInfo;
					}
					finally
					{
						this.buildingSet.Remove(key);

						Monitor.PulseAll(this.buildingSet);
					}
				}
			}
		}
	}
}