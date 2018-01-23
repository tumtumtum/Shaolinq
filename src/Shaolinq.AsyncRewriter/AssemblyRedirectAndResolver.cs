using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter
{
	public class AssemblyRedirectAndResolver
	{
		public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var name = Regex.Match(args.Name, "[^,]*").Value;

			var directories = new List<string>
			{
				Path.GetDirectoryName(typeof(AsyncRewriterTask).Assembly.Location)
			};

			var uri = new Uri(typeof(AsyncRewriterTask).Assembly.CodeBase);

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
