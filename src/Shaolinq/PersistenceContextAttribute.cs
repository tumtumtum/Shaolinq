using System;
using Platform;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
	public class PersistenceContextAttribute
        : Attribute
	{
		public static readonly PersistenceContextAttribute Default = new PersistenceContextAttribute(PersistenceContextDefault.Default);

		public string PersistenceContextName { get; set; }
		public PersistenceContextDefault PersistenceContextDefault { get; set; }

		public PersistenceContextAttribute(PersistenceContextDefault value)
		{
			this.PersistenceContextDefault = value;
		}

		public PersistenceContextAttribute(PersistenceContextDefault value, string persistenceContextName)
		{
			this.PersistenceContextDefault = value;
			this.PersistenceContextName = persistenceContextName;
		}

		public virtual string GetPersistenceContextName(Type type)
		{
			var isdefault = false;

			switch (this.PersistenceContextDefault)
			{
				case PersistenceContextDefault.Default:
					isdefault = true;
					goto case PersistenceContextDefault.Explicit;
				case PersistenceContextDefault.Explicit:
					if (this.PersistenceContextName == null)
					{
						if (isdefault)
						{
							goto case PersistenceContextDefault.AssemblyDefault;
						}
						else
						{
							throw new InvalidOperationException(String.Format("The type {0} has a DataAccessObject attribute that is missing an explicitly set PersistenceContextName", type));
						}
					}
					return this.PersistenceContextName;
				case PersistenceContextDefault.AssemblyDefault:
					string value;

					if ((value = AssemblyDefaultPersistenceContextAttribute.GetDefaultPersistenceContextName(type.Assembly)) == null)
					{
						if (isdefault)
						{
							goto case PersistenceContextDefault.NamespaceTail;
						}
						else
						{
							throw new InvalidOperationException(String.Format("The type {0} specifies the use of an assembly default persistence context but the assembly for type {0} is missing DefaultPersistenceContext attribute", type));
						}
					}
					return value;
				case PersistenceContextDefault.NamespaceTail:

					string namespaceTail;

					if (String.IsNullOrEmpty(type.Namespace))
					{
						throw new InvalidOperationException(String.Format("Type {0} has an empty namespace", type));
					}

					namespaceTail = type.Namespace.SplitAroundCharFromRight('.').Right;

					if (String.IsNullOrEmpty(namespaceTail))
					{
						namespaceTail = type.Namespace;
					}

					return namespaceTail;
				default:
					throw new InvalidOperationException(String.Format("Unexpected PersistenceContextDefault value: {0}", Enum.GetName(typeof(PersistenceContextDefault), this.PersistenceContextDefault)));
			}
		}
	}
}
