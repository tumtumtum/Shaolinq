// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Data;
using System.Data.Common;

namespace Shaolinq.Persistence
{
	public partial class DataReaderWrapper
		: DbDataReader
	{
		public IDataReader Inner { get; }

		public DataReaderWrapper(IDataReader inner)
		{
			this.Inner = inner;
		}

		protected override void Dispose(bool disposing) => this.Inner.Dispose();
		public override string GetName(int i) => this.Inner.GetName(i);
		public override string GetDataTypeName(int i) => this.Inner.GetDataTypeName(i);
		public override Type GetFieldType(int i) => this.Inner.GetFieldType(i);
		public override object GetValue(int i) => this.Inner.GetValue(i);
		public override int GetValues(object[] values) => this.Inner.GetValues(values);
		public override int GetOrdinal(string name) => this.Inner.GetOrdinal(name);
		public override bool GetBoolean(int i) => this.Inner.GetBoolean(i);
		public override byte GetByte(int i) => this.Inner.GetByte(i);
		public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => this.Inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
		public override char GetChar(int i) => this.Inner.GetChar(i);
		public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => this.Inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);
		public override Guid GetGuid(int i) => this.Inner.GetGuid(i);
		public override short GetInt16(int i) => this.Inner.GetInt16(i);
		public override int GetInt32(int i) => this.Inner.GetInt32(i);
		public override long GetInt64(int i) => this.Inner.GetInt64(i);
		public override float GetFloat(int i) => this.Inner.GetFloat(i);
		public override double GetDouble(int i) => this.Inner.GetDouble(i);
		public override string GetString(int i) => this.Inner.GetString(i);
		public override decimal GetDecimal(int i) => this.Inner.GetDecimal(i);
		public override DateTime GetDateTime(int i) => this.Inner.GetDateTime(i);
		public override bool IsDBNull(int i) => this.Inner.IsDBNull(i);
		public override int FieldCount => this.Inner.FieldCount;
		public override object this[int i] => this.Inner[i];
		public override object this[string name] => this.Inner[name];
		public override void Close() => this.Inner.Close();
		public override DataTable GetSchemaTable() => this.Inner.GetSchemaTable();
		public override bool NextResult() => this.Inner.NextResult();
		public override bool Read() => this.Inner.Read();
		public override int Depth => this.Inner.Depth;
		public override bool IsClosed => this.Inner.IsClosed;
		public override int RecordsAffected => this.Inner.RecordsAffected;

		protected override DbDataReader GetDbDataReader(int ordinal)
		{
			return this.Inner.GetData(ordinal) as DbDataReader;
		}

		public override IEnumerator GetEnumerator()
		{
			return (this.Inner as DbDataReader)?.GetEnumerator();
		}

		public override bool HasRows
		{
			get
			{
				var inner = this.Inner as DbDataReader;

				if (inner != null)
				{
					return inner.HasRows;
				}

				throw new NotImplementedException();
			}
		}
	}
}
