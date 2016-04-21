// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Shaolinq.Persistence
{
	internal static class VariableSubstituter
	{
		private static readonly Regex PatternRegex = new Regex(@"\$\((env\\_)?([a-z_A-Z]+?)((_TOLOWER)|(:([^\)]+)))?\)", RegexOptions.Compiled | RegexOptions.IgnoreCase); 

		public static string Substitute(string value, Func<string, string> variableToValue)
		{
			return PatternRegex.Replace(value, match =>
			{
				var result = match.Groups[1].Length != 0 ? Environment.GetEnvironmentVariable(match.Groups[2].Value) : variableToValue(match.Groups[2].Value);

				if (match.Groups[4].Length > 0)
				{
					result = result?.ToLowerInvariant();
				}

				if (match.Groups[6].Length > 0)
				{
					switch (match.Groups[6].Value)
					{
					case "L":
						result = result?.ToLowerInvariant();
						break;
					case "U":
						result = result?.ToUpperInvariant();
						break;
					}
				}

				return result;
			});
		}
		
		[ThreadStatic] private static HashSet<TypeDescriptor> visitedTypes;

		public static string Substitute(string pattern, TypeDescriptor typeDescriptor)
		{
			var root = false;

			if (pattern == null)
			{
				return typeDescriptor.TypeName;
			}

			if (visitedTypes == null)
			{
				root = true;
				visitedTypes = new HashSet<TypeDescriptor>();
			}

			try
			{
				return Substitute(pattern, value =>
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
			finally
			{
				visitedTypes.Remove(typeDescriptor);

				Debug.Assert(!root || (root && visitedTypes.Count == 0));
			}
		}

		[ThreadStatic] private static HashSet<PropertyDescriptor> visitedProperties;

		public static string Substitute(string pattern, PropertyDescriptor propertyDescriptor)
		{
			var root = false;

			if (pattern == null)
			{
				return propertyDescriptor.PropertyName;
			}

			if (visitedProperties == null)
			{
				root = true;
				visitedProperties = new HashSet<PropertyDescriptor>();
			}

			try
			{
				visitedProperties.Add(propertyDescriptor);

				return Substitute(pattern, value =>
				{
					switch (value.ToUpper())
					{
					case "TYPENAME":
						return propertyDescriptor.DeclaringTypeDescriptor.TypeName;
					case "PROPERTYNAME":
						return propertyDescriptor.PropertyName;
					case "PROPERTYTYPENAME":
						return propertyDescriptor.PropertyType.Name;
					case "TABLENAME":
					case "PERSISTED_TYPENAME":
						return propertyDescriptor.DeclaringTypeDescriptor.PersistedName;
					case "COLUMNNAME":
					case "PERSISTED_PROPERTYNAME":
						return propertyDescriptor.PersistedName;
					case "COLUMNTYPENAME":
					case "PERSISTED_PROPERTYTYPENAME":
						return propertyDescriptor.PropertyTypeTypeDescriptor.PersistedName;
					default:
						throw new NotSupportedException(value);
					}
				});
			}
			finally
			{
				visitedProperties.Remove(propertyDescriptor);

				Debug.Assert(!root || (root && visitedProperties.Count == 0));
			}
		}

		private static readonly Regex ReplacementRegex = new Regex(@"((\\\\)+|([^\\])|)\$([0-9]+?)", RegexOptions.Compiled);

		public static string SedTransform(string value, string transformString)
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

				var result = useReplacementRegex ? ReplacementRegex.Replace(replacement, m => match.Groups[int.Parse(m.Groups[4].Value)].Value) : replacement;

				return result;
			}, regexOptions);
		}
	}
}
