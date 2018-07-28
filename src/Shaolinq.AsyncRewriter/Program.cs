// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.AsyncRewriter
{
	public class Program
	{
		public static void Main(string[] args)
		{
			string[] output = null;
			string[] input = null;
			string[] assemblies = null;
			var alwayswrite = false;

			if (args.Length == 1 && args[0] == "@")
			{
				args = CommandLineParser.ParseArguments(Console.ReadLine());
			}

			for (var i = 0; i < args.Length; i++)
			{
				var arg = args[i];

				if (arg == "-output")
				{
					output = args[++i].Split(';');
				}
				else if (arg == "-assemblies")
				{
					assemblies = args[++i].Split(';');
				}
				else if (arg == "-alwayswrite")
				{
					alwayswrite = true;
				}
				else
				{
					input = new string[args.Length - i];

					Array.Copy(args, i, input, 0, args.Length - i);

					break;
				}
			}

			Rewriter.Rewrite(input, assemblies, output, !alwayswrite);
		}
	}
}
