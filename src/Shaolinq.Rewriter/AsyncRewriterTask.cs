using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Shaolinq.Rewriter
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
			var asyncCode = this.rewriter.RewriteAndMerge(InputFiles.Select(f => f.ItemSpec).ToArray(), Assemblies?.Select(c => c.ItemSpec).ToArray());

			File.WriteAllText(OutputFile.ItemSpec, asyncCode);

			return true;
		}
	}
}
