// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence.Computed;

namespace Shaolinq
{
	/// <summary>
	/// Applied to a persistable property to indicate that each new object will automatically be assigned a value.
	/// Only applicable to integer and Guid properties.
	/// </summary>
	/// <remarks>
	/// This property can also be applied on overridden properties to disable the autoincrement attribute by setting the <see cref="AutoIncrement"/> property to false.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property)]
	public class AutoIncrementAttribute
		: Attribute
	{
		public long Step { get; set; } = 1;
		public long Seed { get; set; } = 1;
		public bool AutoIncrement { get; set; }
		public Type ReferencedType { get; set; }
		public Type[] ReferencedTypes { get; set; }
		public string ValidateExpression { get; set; }

		public AutoIncrementAttribute()
			: this(true)    
		{
		}

		public AutoIncrementAttribute(bool autoIncrement)
		{
			this.AutoIncrement = autoIncrement;
		}

		public LambdaExpression GetValidateLambdaExpression(DataAccessModelConfiguration configuration, PropertyInfo propertyInfo)
		{
			return this.ValidateExpression == null ? null : ComputedExpressionParser.Parse(this.ValidateExpression, propertyInfo, ComputedMemberAttribute.GetReferencedTypes(configuration, propertyInfo, this.ReferencedTypes?.ConcatUnlessNull(this.ReferencedType).ToArray()), typeof(bool));
		}
	}
}
