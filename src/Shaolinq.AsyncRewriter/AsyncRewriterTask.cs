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

		private readonly Rewriter rewriter;

		public AsyncRewriterTask()
		{
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
