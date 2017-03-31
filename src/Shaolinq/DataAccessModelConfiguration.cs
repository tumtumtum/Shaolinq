// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Platform.Text;
using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq
{
	[XmlElement]
	public class DataAccessModelConfiguration
	{
		/// <summary>
		/// A list of one of more database connections.
		/// </summary>
		[XmlElement("SqlDatabaseContexts")]
		[XmlListElementDynamicTypeProvider(typeof(SqlDatabaseContextInfoDynamicTypeProvider))]
		public List<SqlDatabaseContextInfo> SqlDatabaseContextInfos { get; set; }

		/// <summary>
		/// Default settings for contraints such as those defined in <seealso cref="Platform.Validation.SizeConstraintAttribute"/>
		/// </summary>
		[XmlElement("ConstraintDefaults")]
		public ConstraintDefaultsConfiguration ConstraintDefaultsConfiguration { get; set; }

		/// <summary>
		/// Configuration for how various names are translated into SQL.
		/// </summary>
		[XmlElement("NamingTransforms")]
		public NamingTransformsConfiguration NamingTransforms { get; set; }

		/// <summary>
		/// A list of types that are impplicitly referenced for the purposes of evaluating expressions
		/// </summary>
		/// <remarks>
		/// <seealso cref="Shaolinq.ComputedMemberAttribute"/>
		/// </remarks>
		[XmlElement("ReferencedTypes")]
		[XmlListElement("Type", ItemType = typeof(Type), SerializeAsValueNode = true, ValueNodeAttributeName = "Name")]
		public List<Type> ReferencedTypes { get; set; }
		
		/// <summary>
		/// By default Shaolinq saves a copy of the generated DataAccessModel and reuses it on demand.
		/// </summary>
		/// <remarks>
		/// Set this property to <c>faldse</c> if you prefer to assemblies to only be generated in memory.
		/// </remarks>
		[XmlAttribute]
		public bool? SaveAndReuseGeneratedAssemblies { get; set; } = true;

		/// <summary>
		/// By default value types are not set to <c>default(T)</c> and will cause
		/// </summary>
		[XmlAttribute]
		public bool ValueTypesAutoImplicitDefault { get; set; } = false;

		/// <summary>
		/// By default properties with declared default values that nare not set are not submitted to 
		/// the database and the database is expected to apply the <c>DEFAULT VALUE</c> constraint
		/// </summary>
		/// <remarks>
		/// Set this propertuy to true if you want values to always be submitted on new objects even
		/// if they are the default value - potentially overriding the <c>DEFAULT VALUE</c> constraint
		/// on the declared database schema if there is a mismatch
		/// </remarks>
		[XmlAttribute]
		public bool AlwaysSubmitDefaultValues { get; set; } = false;

		/// <summary>
		/// Path to the folder to store generated assemblies if <see cref="SaveAndReuseGeneratedAssemblies"/> is <c>true</c>.
		/// </summary>
		[XmlAttribute]
		public string GeneratedAssembliesSaveDirectory { get; set; }

		public DataAccessModelConfiguration()
		{
			this.SqlDatabaseContextInfos = new List<SqlDatabaseContextInfo>();
			this.ConstraintDefaultsConfiguration = new ConstraintDefaultsConfiguration();
		}

		/// <summary>
		/// Get a SHA1 hash of the configuration
		/// </summary>
		public byte[] GetSha1Bytes()
		{
			return SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(XmlSerializer<DataAccessModelConfiguration>.New().SerializeToString(this)));
		}

		/// <summary>
		/// Gets the SHA1 hash of the configuration has a series of hex encoded bytes.
		/// </summary>
		public string GetSha1Hex()
		{
			return TextConversion.ToHexString(this.GetSha1Bytes());
		}
	}
}
