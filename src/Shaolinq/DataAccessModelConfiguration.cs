// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Platform.Text;
using Platform.Validation;
using Platform.Xml.Serialization;
using Shaolinq.Persistence;

namespace Shaolinq
{
	/// <summary>
	/// Represents the configuration of a model
	/// </summary>
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
		/// By default value types are set to <c>default(T)</c> as is the case with C#.
		/// Set this property to false to disable this behaviour and require defaults to be set.
		/// </summary>
		[XmlAttribute]
		public bool ValueTypesAutoImplicitDefault { get; set; } = true;

		/// <summary>
		/// By default, properties with declared default values that are not explicitly set are will besubmitted to 
		/// the database. Set this to false to not send default values and have the database apply the default value
		/// basded upon the <c>DEFAULT VALUE</c> constraint.
		/// </summary>
		/// <remarks>
		/// The default for this property is L<c>true</c>.
		/// Set this propertuy to false if you want default values to be ommitted when submitting new objects.
		/// When this property is true (default) then the default value that is configured on the DataAccessModel
		/// will override the <c>DEFAULT VALUE</c> constraint declared database schema if there is a schema mismatch.
		/// <para>
		/// This property is ignored if <see cref="IncludeImplicitDefaultsInSchema"/> is false.
		/// </para>
		/// </remarks>
		[XmlAttribute]
		public bool AlwaysSubmitDefaultValues { get; set; } = true;

		/// <summary>
		/// Include the implict default values into the database schema.
		/// </summary>
		/// <remarks>
		/// The default value for this property is false which means implicit default values are not included in the
		/// database schema as a <c>DEFAULT VALUE</c> directive. Explicitly declared default values (values declared
		/// using the <see cref="DefaultValueAttribute"/>) are always included in the schema.
		/// <para>
		/// This property only has an effect if <see cref="ValueTypesAutoImplicitDefault"/> is <c>true</c></para>
		/// </remarks>
		[XmlAttribute]
		public bool IncludeImplicitDefaultsInSchema { get; set; } = false;

		/// <summary>
		/// Path to the folder to store generated assemblies if <see cref="SaveAndReuseGeneratedAssemblies"/> is <c>true</c>.
		/// </summary>
		/// <remarks>
		/// If not configured, the directory containing the <c>Shaolinq.dll</c> assembly.
		/// Use <c>$(env.TEMP)</c> if you want to use the system defined TEMP directory (on Azure WebApps for example)
		/// </remarks>
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
