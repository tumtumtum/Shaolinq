// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;
using System.Linq;
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

		public AsyncRewriterTask()
		{
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
