// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

 using System.Collections.Generic;
using System.IO;

namespace Shaolinq.Persistence
{
	public class MigrationScripts
	{
		private readonly Dictionary<string, string> scripts;

		public MigrationScripts()
		{
			this.scripts = new Dictionary<string, string>();
		}

		public void AddScripts(DataAccessModel model, MigrationScripts other)
		{
			foreach (var keyValuePair in other.scripts)
			{
				this.scripts[keyValuePair.Key] = keyValuePair.Value;
			}
		}

		public void AddScript(DataAccessModel model, TypeDescriptor typeDescriptor, string script)
		{
			this.scripts[typeDescriptor.GetPersistedName(model)] = script;
		}

		public IEnumerable<KeyValuePair<string, string>> GetScripts()
		{
			return scripts;
		}

		public void WriteScripts(TextWriter writer)
		{
			foreach (var s in this.scripts.Values)
			{
				writer.WriteLine(s);
			}
		}

		public void WriteScripts(string outputPath)
		{
			foreach (var keyValuePair in this.scripts)
			{
				File.WriteAllText(Path.Combine(outputPath, keyValuePair.Key), keyValuePair.Value);
			}
		}
	}
}
