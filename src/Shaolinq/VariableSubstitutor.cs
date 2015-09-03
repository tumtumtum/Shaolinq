// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Shaolinq
{
	internal static class VariableSubstitutor
	{
		private static readonly Regex PatternRegex = new Regex(@"\$\([a-z_A-Z]+\)", RegexOptions.Compiled); 

		public static string Substitute(string value, Func<string, string> variableToValue)
		{
			return PatternRegex.Replace(value, match => variableToValue(match.Groups[0].Value));
		}
	}
}
