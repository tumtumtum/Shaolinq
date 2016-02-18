// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;

namespace Shaolinq.Persistence
{
	public class DataReaderWrapper
		: IDataReader
	{
		public IDataReader Inner { get; }

		public DataReaderWrapper(IDataReader inner)
		{
			this.Inner = inner;
		}

		public virtual void Dispose() => this.Inner.Dispose();
		public virtual string GetName(int i) => this.Inner.GetName(i);
		public virtual string GetDataTypeName(int i) => this.Inner.GetDataTypeName(i);
		public virtual Type GetFieldType(int i) => this.Inner.GetFieldType(i);
		public virtual object GetValue(int i) => this.Inner.GetValue(i);
		public virtual int GetValues(object[] values) => this.Inner.GetValues(values);
		public virtual int GetOrdinal(string name) => this.Inner.GetOrdinal(name);
		public virtual bool GetBoolean(int i) => this.Inner.GetBoolean(i);
		public virtual byte GetByte(int i) => this.Inner.GetByte(i);
		public virtual long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => this.Inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
		public virtual char GetChar(int i) => this.Inner.GetChar(i);
		public virtual long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => this.Inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);
		public virtual Guid GetGuid(int i) => this.Inner.GetGuid(i);
		public virtual short GetInt16(int i) => this.Inner.GetInt16(i);
		public virtual int GetInt32(int i) => this.Inner.GetInt32(i);
		public virtual long GetInt64(int i) => this.Inner.GetInt64(i);
		public virtual float GetFloat(int i) => this.Inner.GetFloat(i);
		public virtual double GetDouble(int i) => this.Inner.GetDouble(i);
		public virtual string GetString(int i) => this.Inner.GetString(i);
		public virtual decimal GetDecimal(int i) => this.Inner.GetDecimal(i);
		public virtual DateTime GetDateTime(int i) => this.Inner.GetDateTime(i);
		public virtual IDataReader GetData(int i) => this.Inner.GetData(i);
		public virtual bool IsDBNull(int i) => this.Inner.IsDBNull(i);
		public virtual int FieldCount => this.Inner.FieldCount;
		public virtual object this[int i] => this.Inner[i];
		public virtual object this[string name] => this.Inner[name];
		public virtual void Close() => this.Inner.Close();
		public virtual DataTable GetSchemaTable() => this.Inner.GetSchemaTable();
		public virtual bool NextResult() => this.Inner.NextResult();
		public virtual bool Read() => this.Inner.Read();
		public virtual int Depth => this.Inner.Depth;
		public virtual bool IsClosed => this.Inner.IsClosed;
		public virtual int RecordsAffected => this.Inner.RecordsAffected;
	}
}
