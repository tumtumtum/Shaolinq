// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Platform;

namespace Shaolinq.Persistence
{
	internal static class VariableSubstituter
	{
		private static readonly Regex PatternRegex = new Regex(@"(?<prefix>^|[^\\\$]*|[\\\\]+)(\$(?<name>[0-9])+|\$\((?<env>env_)?(?<name>[a-z_A-Z]+?)((?<tolower>_TOLOWER)|(:(?<format>[^\)]+)))?\))", RegexOptions.Compiled | RegexOptions.IgnoreCase); 

		public static string Substitute(string value, Func<string, object> variableToValue)
		{
			return PatternRegex.Replace(value, match =>
			{
				var result = match.Groups["env"].Length != 0 ? Environment.GetEnvironmentVariable(match.Groups["name"].Value) : variableToValue(match.Groups["name"].Value);

				if (match.Groups["tolower"].Length > 0 && result is string)
				{
					result = ((string)result).ToLowerInvariant();
				}

				var format = match.Groups["format"].Value;

				if (format.Length > 0)
				{
					switch (format)
					{
					case "L":
						if (result is string)
						{
							result = ((string)result).ToLowerInvariant();
						}
						break;
					case "U":
						if (result is string)
						{
							result = ((string)result).ToUpperInvariant();
						}	
						break;
					case "_":
						result = string.Join("_", (result as IEnumerable).ToTyped<object>()?.Select(c => c.ToString()).ToArray());
						break;
					}
				}

				if (result is IEnumerable && !(result is string))
				{
					result = string.Join("_", (result as IEnumerable).ToTyped<object>()?.Select(c => c.ToString()).ToArray());
				}

				return match.Groups["prefix"].Value + result;
			});
		}
		
		public static string Substitute(string input, TypeDescriptor typeDescriptor)
		{
			if (input == null)
			{
				return typeDescriptor.TypeName;
			}

			var visitedTypes = new HashSet<TypeDescriptor>();
			
			return Substitute(input, value =>
			{
				switch (value.ToUpper())
				{
				case "TYPENAME":
					return typeDescriptor.TypeName;
				case "TABLENAME":
				case "PERSISTED_TYPENAME":
					if (visitedTypes.Contains(typeDescriptor))
					{
						throw new InvalidOperationException("Recursive variable substitution");
					}
					visitedTypes.Add(typeDescriptor);
					return typeDescriptor.PersistedName;
				default:
					throw new NotSupportedException(value);
				}
			});
		}

		public static string Substitute(string input, PropertyDescriptor property)
		{
			return Substitute(input, new[] { property });
		}

		public static string Substitute(string input, PropertyDescriptor[] properties = null, Func<int, string> indexedToValue = null)
		{
			if (input == null)
			{
				return properties.FirstOrDefault()?.PropertyName;
			}

			return Substitute(input, value =>
			{
				var s = value.ToUpper();

				if (properties.Length > 0)
				{
					var property = properties[0];

					switch (s)
					{
					case "TYPENAME":
						return property.DeclaringTypeDescriptor.TypeName;
					case "PROPERTYNAME":
						return property.PropertyName;
					case "PROPERTYTYPENAME":
						return property.PropertyType.Name;
					case "TABLENAME":
					case "PERSISTED_TYPENAME":
						return property.DeclaringTypeDescriptor.PersistedName;
					case "COLUMNNAME":
					case "PERSISTED_PROPERTYNAME":
						return property.PersistedName;
					case "COLUMNTYPENAME":
					case "PERSISTED_PROPERTYTYPENAME":
						return property.PropertyTypeTypeDescriptor.PersistedName;
					case "PROPERTYNAMES":
						return properties.Select(c => c.PropertyName);
					case "COLUMNNAMES":
						return properties.Select(c => c.PersistedName);
					}
				}

				int number;

				if (indexedToValue != null && int.TryParse(s, out number))
				{
					return indexedToValue(number);
				}

				throw new NotSupportedException(value);
			});
		}

		public static string SedTransform(string value, string transformString, PropertyDescriptor property)
		{
			return SedTransform(value, transformString, new[] { property });
		}

		public static string SedTransform(string value, string transformString, PropertyDescriptor[] properties = null)
		{
			if (string.IsNullOrEmpty(transformString))
			{
				return value;
			}

			if (transformString.Length < 4 || transformString[0] != 's')
			{
				throw new ArgumentException(nameof(transformString));
			}

			var c = transformString[1];
			transformString = transformString.Substring(2);
			var ss = transformString.Split(c);

			if (ss.Length != 3)
			{
				throw new ArgumentException(nameof(transformString));
			}

			var pattern = ss[0];
			var replacement = ss[1];
			var options = ss[2];
			var useReplacementRegex = replacement.Contains("$");

			var maxCount = 1;
			var regexOptions = RegexOptions.None;

			if (options.Contains("g"))
			{
				maxCount = int.MaxValue;
			}

			if (options.Contains("i"))
			{
				regexOptions |= RegexOptions.IgnoreCase;
			}

			var count = 0;

			return Regex.Replace(value, pattern, match =>
			{
				if (maxCount != int.MaxValue && count > maxCount)
				{
					return match.Value;
				}

				count++;

				var result = useReplacementRegex ? Substitute(replacement, properties, index => match.Groups[index].Value) : replacement;

				return result;
			}, regexOptions);
		}
	}
}
