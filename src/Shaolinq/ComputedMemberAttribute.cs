// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence.Computed;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ComputedMemberAttribute
		: PersistedMemberAttribute
	{
		public string GetExpression { get; set; }
		public string SetExpression { get; set; }
		public Type ReferencedType { get; set; }
		public Type[] ReferencedTypes { get; set; }
		public bool AllowExplicitSet { get; set; }
		
		public ComputedMemberAttribute(string getExpression, string setExpression = null)
		{
			this.GetExpression = getExpression;
			this.SetExpression = setExpression;
		}

		internal static Type[] GetReferencedTypes(DataAccessModelConfiguration configuration, PropertyInfo propertyInfo, Type[] referencedTypes)
		{
			var retval = new List<Type>();

			if (configuration.ReferencedTypes != null)
			{
				retval.AddRange(configuration.ReferencedTypes);
			}

			if (referencedTypes != null)
			{
				retval.AddRange(referencedTypes);
			}

			if (propertyInfo?.PropertyType != null)
			{
				retval.Add(propertyInfo.PropertyType);
			}

			if (propertyInfo?.DeclaringType != null)
			{
				retval.Add(propertyInfo.DeclaringType);
			}

			return retval.ToArray();
		}

		public LambdaExpression GetGetLambdaExpression(DataAccessModelConfiguration configuration, PropertyInfo propertyInfo)
		{
			return this.GetExpression == null ? null : ComputedExpressionParser.Parse(this.GetExpression, propertyInfo, GetReferencedTypes(configuration, propertyInfo, this.ReferencedTypes?.ConcatUnlessNull(this.ReferencedType).ToArray()), propertyInfo.PropertyType);
		}

		public LambdaExpression GetSetLambdaExpression(DataAccessModelConfiguration configuration, PropertyInfo propertyInfo)
		{
			return this.SetExpression == null ? null : ComputedExpressionParser.Parse(this.SetExpression, propertyInfo, GetReferencedTypes(configuration, propertyInfo, this.ReferencedTypes?.ConcatUnlessNull(this.ReferencedType).ToArray()), propertyInfo.PropertyType);
		}
	}
}