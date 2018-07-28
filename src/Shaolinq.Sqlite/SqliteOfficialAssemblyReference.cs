// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Data.SQLite;

namespace Shaolinq.Sqlite
{
	/// <summary>
	/// This class is required to force VS to include the NuGet supplied System.Data.SQLite.dll
	/// assembly in a referencing project's output bin directory.
	/// </summary>
	internal class SqliteOfficialAssemblyReference
	{
		internal static SQLiteConnection connection = null;
	}
}
