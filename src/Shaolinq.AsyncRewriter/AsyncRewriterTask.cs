// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;

namespace Shaolinq.AsyncRewriter
{
	public class AsyncRewriterTask : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string[] InputFiles { get; set; }

		[Required]
		public string[] OutputFile { get; set; }

		public string[] Assemblies { get; set; }

		public bool DontWriteIfNoChanges { get; set; }

		private readonly Rewriter rewriter;

		public AsyncRewriterTask()
		{
			this.rewriter = new Rewriter();
		}

		public override bool Execute()
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = this.GetType().Assembly.Location,
				UseShellExecute = false,
				CreateNoWindow = false,
				RedirectStandardOutput = false,
				RedirectStandardError = false,
				RedirectStandardInput = true,
				Arguments = @"@"
			};

			var process = new Process { StartInfo = startInfo };

			process.Start();

			using (var writer = process.StandardInput)
			{
				writer.Write($"-output \"{string.Join(";", this.OutputFile)}\" -assemblies \"{string.Join(";", this.Assemblies)}\" {(DontWriteIfNoChanges == false ? "-alwayswrite" : "")} ");

				for (var i = 0; i < this.InputFiles.Length; i++)
				{
					var file = this.InputFiles[i];

					writer.Write(file);

					if (i != this.InputFiles.Length - 1)
					{ 
						writer.Write(' ');
					}
					else
					{
						writer.WriteLine();
					}
				}

				process.WaitForExit();
			}

			return process.ExitCode == 0;
		}
	}
}
