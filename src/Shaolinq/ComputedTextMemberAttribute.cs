// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Shaolinq
{
	public class ComputedTextMemberAttribute
		: PersistedMemberAttribute
	{
		internal static readonly Regex FormatRegex = new Regex(@"\{([^\}:]+)(:([^\}]+))?\}", RegexOptions.Compiled);

		public string Format { get; set; }

		public ComputedTextMemberAttribute(string format)
		{
			this.Format = format;
		}

		public IEnumerable<string> GetPropertyReferences()
		{
			var matches = FormatRegex.Matches(this.Format);

			return from Match match in matches select match.Groups[1].Value;
		}
	}
}
