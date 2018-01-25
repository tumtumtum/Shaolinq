using System;

namespace Shaolinq.ExpressionWriter
{
	public class Program
	{
		public static void Main(string[] args)
		{
			string output = null;
			string writer = null;
			string[] input = null;
			
			for (var i = 0; i < args.Length; i++)
			{
				var arg = args[i];

				if (arg == "-writer")
				{
					writer = args[++i];
				}
				else if (arg == "-output")
				{
					output = args[++i];
				}
				else
				{
					input = new string[args.Length - i];

					Array.Copy(args, i, input, 0, args.Length - i);

					break;
				}
			}
		
			if (writer == "comparer")
			{
				ExpressionComparerWriter.Write(input, output);
			}
			else if (writer == "hasher")
			{
				ExpressionHasherWriter.Write(input, output);
			}
		}
	}
}
