using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;

namespace Shaolinq.AsyncRewriter
{
	public class AsyncRewriterTask : Microsoft.Build.Utilities.Task
	{
		[Required]
		public ITaskItem[] InputFiles { get; set; }

		[Required]
		public ITaskItem OutputFile { get; set; }

		public ITaskItem[] Assemblies { get; set; }

		private readonly Rewriter rewriter;
		
		private static IEnumerable<string> GetDlls(string parentDirectory)
		{
			foreach (var file in Directory.GetFiles(parentDirectory, "*.dll"))
			{
				yield return file;
			}

			foreach (var directory in Directory.GetDirectories(parentDirectory))
			{
				foreach (var file in GetDlls(Path.Combine(parentDirectory, directory)))
				{
					yield return file;
				}
			}
		}
		
		public AsyncRewriterTask()
		{
			var assemblyPath = this.GetType().Assembly.Location;
			
			var components =
				Path.GetDirectoryName(assemblyPath)
				.Split(Path.DirectorySeparatorChar)
				.Reverse()
				.SkipWhile(c => !c.Equals("packages", StringComparison.InvariantCultureIgnoreCase))
				.Reverse()
				.ToList();

			var path = string.Join(Path.DirectorySeparatorChar.ToString(), components);

			if (path != "")
			{
				var pathsByFileName = new Dictionary<string, string>();

				foreach (var value in GetDlls(path))
				{
					var fileName = Path.GetFileName(value);

					if (fileName != null)
					{
						pathsByFileName[fileName] = value;
					}
				}

				AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
				{
					var fileName = e.Name.Split(',')[0] + ".dll";

					if (pathsByFileName.TryGetValue(fileName, out path))
					{
						if (File.Exists(path))
						{
							return Assembly.LoadFrom(path);
						}
					}

					return null;
				};
			}

			this.rewriter = new Rewriter();
		}

		public override bool Execute()
		{
			var asyncCode = this.rewriter.RewriteAndMerge(this.InputFiles.Select(f => f.ItemSpec).ToArray(), this.Assemblies?.Select(c => c.ItemSpec).ToArray());

			File.WriteAllText(this.OutputFile.ItemSpec, asyncCode);

			return true;
		}
	}
}
