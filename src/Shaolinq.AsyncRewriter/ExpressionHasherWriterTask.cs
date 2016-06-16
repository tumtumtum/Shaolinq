using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Shaolinq.Rewriter
{
	public class ExpressionHasherWriterTask: Microsoft.Build.Utilities.Task
	{
		[Required]
		public ITaskItem[] InputFiles { get; set; }

		[Required]
		public ITaskItem OutputFile { get; set; }

		public override bool Execute()
		{
			var result = ExpressionHasherWriter.Write(InputFiles.Select(c => c.ItemSpec).ToArray());

			File.WriteAllText(OutputFile.ItemSpec, result);

			return true;
		}
	}
}
