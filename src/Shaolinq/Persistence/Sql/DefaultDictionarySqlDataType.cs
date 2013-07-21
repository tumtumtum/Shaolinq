using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Platform;
using Platform.Text;

namespace Shaolinq.Persistence.Sql
{
	public class DefaultDictionarySqlDataType<K, V>
		: DefaultStringSqlDataType
	{
		public DefaultDictionarySqlDataType(Type supportedType)
			: base(supportedType)
		{
		}

		public override Pair<Type, object> ConvertForSql(object value)
		{
			if (value == null)
			{
				return new Pair<Type, object>(typeof(string), "");
			}

			return new Pair<Type, object>(typeof(string), DictionaryToString(((ShoalinqDictionary<K, V>)value)));
		}

		private const string StringsToEscape = "%|+;=";

		public static string DictionaryToString(ShoalinqDictionary<K, V> dictionary)
		{
			var builder = new StringBuilder();

			foreach (var keyValuePair in dictionary)
			{
				builder.Append(TextConversion.ToEscapedHexString(Convert.ToString(keyValuePair.Key), StringsToEscape));
				builder.Append('=');
				builder.Append(TextConversion.ToEscapedHexString(Convert.ToString(keyValuePair.Value), StringsToEscape));
				builder.Append(';');
			}

			if (builder.Length > 0)
			{
				builder.Length--;
			}

			return builder.ToString();
		}

		private static readonly MethodInfo ReaderToDictionaryMethod = typeof(DefaultDictionarySqlDataType<K, V>).GetMethod("ReaderToDictionary");

		public static ShoalinqDictionary<K, V> ReaderToDictionary(IDataReader dataReader, int ordinal)
		{
			if (dataReader.IsDBNull(ordinal))
			{
				return new ShoalinqDictionary<K, V>();
			}
			else
			{
				var retval = new ShoalinqDictionary<K, V>();
				var stringValue = dataReader.GetString(ordinal);

				if (stringValue.Length == 0)
				{
					return retval;
				}
				
				foreach (string s in stringValue.Split(';'))
				{
					var keyValue = s.Split('=');

					var key = TypeHelper.ConvertValue<K>(TextConversion.FromEscapedHexString(keyValue[0]));
					var value = TypeHelper.ConvertValue<V>(TextConversion.FromEscapedHexString(keyValue[1]));

					retval[key] = value;
				}

				retval.Changed = false;

				return retval;
			}
		}

		public override Expression GetReadExpression(ParameterExpression objectProjector, ParameterExpression dataReader, int ordinal)
		{
			return Expression.Call(null, ReaderToDictionaryMethod, dataReader, Expression.Constant(ordinal));
		}
	}
}
