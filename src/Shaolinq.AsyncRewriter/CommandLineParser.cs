using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shaolinq.AsyncRewriter
{
	public class CommandLineParser
	{
		public static string[] ParseArguments(string commandLine)
		{
			return PrivateParseArguments(commandLine).ToArray();
		}

		private static IEnumerable<string> PrivateParseArguments(string commandLine)
		{
			var insideQuote = false;
			var current = new StringBuilder();

			for (var i = 0; i < commandLine.Length; i++)
			{
				if (insideQuote)
				{
					switch (commandLine[i])
					{
					case '\\':
						if (commandLine[i + 1] == '"')
						{
							current.Append('"');
						}
						else
						{
							current.Append('\\');
						}
						continue;
					case '"':
						if (i == commandLine.Length - 1 || commandLine[i + 1] == ' ')
						{
							insideQuote = false;
							if (current.Length > 0)
							{
								yield return current.ToString();
								current.Length = 0;
							}
						}
						continue;
					default:
						current.Append(commandLine[i]);
						continue;
					}
				}

				switch (commandLine[i])
				{
				case ' ':
					if (current.Length > 0)
					{
						yield return current.ToString();
						current.Length = 0;
					}
					break;
				case '"':
					if (i == 0 || commandLine[i - 1] == ' ')
					{
						insideQuote = true;
						continue;
					}
					break;
				default:
					current.Append(commandLine[i]);
					continue;
				}
			}

			if (current.Length > 0)
			{
				yield return current.ToString();
			}
		}
	}
}
