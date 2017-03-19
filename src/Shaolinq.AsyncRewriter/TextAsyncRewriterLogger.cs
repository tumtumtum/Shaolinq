// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;

namespace Shaolinq.AsyncRewriter
{
	public class TextAsyncRewriterLogger: IAsyncRewriterLogger
	{
		public static readonly TextAsyncRewriterLogger ConsoleLogger = new TextAsyncRewriterLogger(Console.Out, Console.Error);

		private readonly TextWriter outputWriter;
		private readonly TextWriter errorWriter;

		public TextAsyncRewriterLogger(TextWriter outputWriter, TextWriter errorWriter)
		{
			this.outputWriter = outputWriter;
			this.errorWriter = errorWriter;
		}

		public void LogWarning(string text)
		{
			this.errorWriter.WriteLine(text);
		}

		public void LogError(string text)
		{
			this.errorWriter.WriteLine(text);
		}

		public void LogMessage(string text)
		{
			this.outputWriter.WriteLine(text);
		}
	}
}