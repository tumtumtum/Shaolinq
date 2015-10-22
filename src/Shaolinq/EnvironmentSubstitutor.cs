// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Shaolinq
{
	public class EnvironmentSubstitutor
	{
		private static readonly Regex Regex = new Regex(@"\$\(([^)]+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public static string Substitute(string value)
		{
			return Regex.Replace(value, match => GetVariable(match.Groups[1].Value));
		}

		private static string GetVariable(string name)
		{
			var retval = Environment.GetEnvironmentVariable(name);

			if (retval == null)
			{
				var propertyInfo = typeof (Environment).GetProperty(name, BindingFlags.Static | BindingFlags.Public);

				if (propertyInfo != null)
				{
					retval = (string)propertyInfo.GetValue(null, null);
				}
			}

			return retval;
		}
	}
}
