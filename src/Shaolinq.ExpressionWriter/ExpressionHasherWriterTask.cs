// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Shaolinq.ExpressionWriter
{
	public class ExpressionHasherWriterTask: Microsoft.Build.Utilities.Task
	{
		[Required]
		public ITaskItem[] InputFiles { get; set; }

		[Required]
		public ITaskItem OutputFile { get; set; }

		public override bool Execute()
		{
			var result = ExpressionHasherWriter.Write(this.InputFiles.Select(c => c.ItemSpec).ToArray());

			File.WriteAllText(this.OutputFile.ItemSpec, result);

			return true;
		}
	}
}
