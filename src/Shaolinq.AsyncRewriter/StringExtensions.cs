// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.AsyncRewriter
{
	public static class StringExtensions
	{
		public static string Left(this string s, Predicate<char> acceptChar)
		{
			int i;

			for (i = 0; i < s.Length; i++)
			{
				if (!acceptChar(s[i]))
				{
					break;
				}
			}

			return i >= s.Length ? s : s.Substring(0, i);
		}

		public static string Right(this string s, Predicate<char> acceptChar)
		{
			int i;

			for (i = s.Length - 1; i >= 0; i--)
			{
				if (!acceptChar(s[i]))
				{
					break;
				}
			}

			return i < 0 ? s : s.Substring(i + 1);
		}
	}
}
