// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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

		public bool DontWriteIfNoChanges { get; set; }

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

		private static bool Equal(TextReader left, TextReader right)
		{
			while (true)
			{
				var l = left.Read();
				var r = right.Read();

				if (l != r)
				{
					return false;
				}

				if (l == -1)
				{
					return true;
				}
			}
		}

		public override bool Execute()
		{
			var changed = true;
			var filename = this.OutputFile.ItemSpec;
			var code = this.rewriter.RewriteAndMerge(this.InputFiles.Select(f => f.ItemSpec).ToArray(), this.Assemblies?.Select(c => c.ItemSpec).ToArray());

			try
			{
				using (TextReader left = File.OpenText(filename), right = new StringReader(code))
				{
					if (Equal(left, right))
					{
						this.Log.LogMessage($"{filename} has not changed");

						changed = false;
					}
				}
			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException(e);
			}

			if (!changed && this.DontWriteIfNoChanges)
			{
				return true;
			}

			File.WriteAllText(filename, code);

			return true;
		}
	}
}
