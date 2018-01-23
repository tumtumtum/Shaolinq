using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Shaolinq.ExpressionWriter
{
	public class AssemblyRedirectAndResolver
	{
		public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var name = Regex.Match(args.Name, "[^,]*").Value;

			var directories = new List<string>
			{
				Path.GetDirectoryName(typeof(ExpressionHasherWriterTask).Assembly.Location)
			};

			var uri = new Uri(typeof(ExpressionHasherWriterTask).Assembly.CodeBase);

			if (uri.Scheme == "file")
			{
				directories.Add(Path.GetDirectoryName(uri.LocalPath));
			}

			foreach (var directory in directories)
			{
				var current = Path.Combine(directory, name + ".dll");

				if (File.Exists(current))
				{
					return Assembly.LoadFrom(current);
				}
			}

			return null;
		}
	}
}
