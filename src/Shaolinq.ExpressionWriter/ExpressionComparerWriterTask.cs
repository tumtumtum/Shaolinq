// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Shaolinq.ExpressionWriter
{
	public class ExpressionComparerWriterTask : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string[] InputFiles { get; set; }

		[Required]
		public string OutputFile { get; set; }

		public override bool Execute()
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = this.GetType().Assembly.Location,
				UseShellExecute = false,
				CreateNoWindow = false,
				RedirectStandardOutput = false,
				RedirectStandardError = false,
				RedirectStandardInput = false,
				Arguments = $"-writer comparer -output {this.OutputFile} {string.Join(" ", this.InputFiles)}"
			};

			var process = new Process { StartInfo = startInfo };
			
			process.Start();
			process.WaitForExit();
			
			return process.ExitCode == 0;
		}
	}
}
