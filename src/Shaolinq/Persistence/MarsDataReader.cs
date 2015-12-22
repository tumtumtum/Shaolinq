using System;
using System.Collections.Generic;
using System.Data;

namespace Shaolinq.Persistence
{
	public class MarsDataReader
		: DataReaderWrapper
	{
		private struct RowData
		{
			public static readonly RowData Empty = new RowData();

			public readonly string[] names;
			public readonly object[] values;

			public RowData(int fieldCount)
				: this()
			{
				this.names = new string[fieldCount];
				this.values = new object[fieldCount];
			}
		}

		private int recordsAffected;
		private int fieldCount;
		private RowData currentRow;
		private Queue<RowData> rows;
		private readonly MarsDbCommand command;
		private Type[] fieldTypes;
		private string[] dataTypeNames;
		private Dictionary<string, int> ordinalByFieldName;
		private bool closed;

		public MarsDataReader(MarsDbCommand command, IDataReader inner)
			: base(inner)
		{
			command.context.currentReader = this;

			this.command = command;
		}

		public void BufferAll()
		{
			if (this.IsClosed || this.closed)
			{
				return;
			}

			rows = new Queue<RowData>();

			try
			{
				var x = 0;

				while (base.Read())
				{
					var rowData = new RowData(base.FieldCount);

					if (x == 0)
					{
						fieldCount = base.FieldCount;
						recordsAffected = base.RecordsAffected;

						this.ordinalByFieldName = new Dictionary<string, int>(fieldCount);
						this.dataTypeNames = new string[fieldCount];
						this.fieldTypes = new Type[fieldCount];
					}

					base.GetValues(rowData.values);

					for (var i = 0; i < base.FieldCount; i++)
					{
						var name = base.GetName(i);

						rowData.names[i] = name;

						if (x == 0)
						{
							ordinalByFieldName[name] = i;
							dataTypeNames[i] = base.GetDataTypeName(i);
							fieldTypes[i] = base.GetFieldType(i);
						}
					}

					rows.Enqueue(rowData);

					x++;
				}
			}
			finally
			{
				this.Dispose();
			}
		}

		public override bool NextResult()
		{
			if (this.rows == null)
			{
				return base.NextResult();
			}

			throw new NotImplementedException();
		}

		public override bool Read()
		{
			if (rows == null)
			{
				return base.Read();
			}

			if (rows.Count == 0)
			{
				currentRow = RowData.Empty;

				return false;
			}

			currentRow = rows.Dequeue();

			return true;
		}

		public override int Depth
		{
			get
			{
				if (this.rows == null)
				{
					return base.Depth;
				}

				throw new NotImplementedException();
			}
		}

		public override void Dispose()
		{
			if (this.command.context.currentReader == this)
			{
				this.command.context.currentReader = null;

				base.Dispose();
			}
		}

		public override string GetName(int i)
		{
			if (rows == null)
			{
				return base.GetName(i);
			}

			return currentRow.names[i];
		}

		public override string GetDataTypeName(int i)
		{
			if (rows == null)
			{
				return base.GetDataTypeName(i);
			}

			return dataTypeNames[i];
		}

		public override Type GetFieldType(int i)
		{
			if (rows == null)
			{
				return base.GetFieldType(i);
			}

			return fieldTypes[i];
		}

		public override object GetValue(int i)
		{
			if (rows == null)
			{
				return base.GetValue(i);
			}

			return currentRow.values[i];
		}

		public override int GetValues(object[] values)
		{
			if (rows == null)
			{
				return base.GetValues(values);
			}

			var x = Math.Min(currentRow.values.Length, values.Length);

			Array.Copy(currentRow.values, values, x);

			return x;
		}

		public override int GetOrdinal(string name)
		{
			if (rows == null)
			{
				return base.GetOrdinal(name);
			}

			return ordinalByFieldName[name];
		}

		public override bool GetBoolean(int i)
		{
			if (rows == null)
			{
				return base.GetBoolean(i);
			}

			return Convert.ToBoolean(currentRow.values[i]);
		}

		public override byte GetByte(int i)
		{
			if (rows == null)
			{
				return base.GetByte(i);
			}

			return Convert.ToByte(currentRow.values[i]);
		}

		public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			if (rows == null)
			{
				return base.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
			}

			var bytes = (byte[])currentRow.values[i];

			var x = Math.Min(length, bytes.Length - fieldOffset);

			if (length < 0 || length > x)
			{
				throw new ArgumentOutOfRangeException(nameof(length));
			}

			Array.Copy(bytes, fieldOffset, buffer, bufferoffset, length);

			return length;
		}

		public override char GetChar(int i)
		{
			if (rows == null)
			{
				return base.GetChar(i);
			}

			return Convert.ToChar(currentRow.values[i]);
		}

		public override long GetChars(int i, long fieldOffset, char[] buffer, int bufferoffset, int length)
		{
			if (rows == null)
			{
				return base.GetChars(i, fieldOffset, buffer, bufferoffset, length);
			}

			var bytes = (byte[])currentRow.values[i];

			var x = Math.Min(length, bytes.Length - fieldOffset);

			if (length < 0 || length > x)
			{
				throw new ArgumentOutOfRangeException(nameof(length));
			}

			Array.Copy(bytes, fieldOffset, buffer, bufferoffset, length);

			return length;
		}

		public override Guid GetGuid(int i)
		{
			if (rows == null)
			{
				return base.GetGuid(i);
			}

			return (Guid)currentRow.values[i];
		}

		public override short GetInt16(int i)
		{
			if (rows == null)
			{
				return base.GetInt16(i);
			}

			return Convert.ToInt16(currentRow.values[i]);
		}

		public override int GetInt32(int i)
		{
			if (rows == null)
			{
				return base.GetInt32(i);
			}

			return Convert.ToInt32(currentRow.values[i]);
		}

		public override long GetInt64(int i)
		{
			if (rows == null)
			{
				return base.GetInt64(i);
			}

			return Convert.ToInt64(currentRow.values[i]);
		}

		public override float GetFloat(int i)
		{
			if (rows == null)
			{
				return base.GetFloat(i);
			}

			return Convert.ToSingle(currentRow.values[i]);
		}

		public override double GetDouble(int i)
		{
			if (rows == null)
			{
				return base.GetDouble(i);
			}

			return Convert.ToDouble(currentRow.values[i]);
		}

		public override string GetString(int i)
		{
			if (rows == null)
			{
				return base.GetString(i);
			}

			return Convert.ToString(currentRow.values[i]);
		}

		public override decimal GetDecimal(int i)
		{
			if (rows == null)
			{
				return base.GetDecimal(i);
			}

			return Convert.ToDecimal(currentRow.values[i]);
		}

		public override DateTime GetDateTime(int i)
		{
			if (this.rows == null)
			{
				return base.GetDateTime(i);
			}

			return Convert.ToDateTime(currentRow.values[i]);
		}

		public override IDataReader GetData(int i)
		{
			if (this.rows == null)
			{
				return base.GetData(i);
			}

			throw new NotImplementedException();
		}

		public override bool IsDBNull(int i)
		{
			if (this.rows == null)
			{
				return base.IsDBNull(i);
			}

			return this.currentRow.values[i] == DBNull.Value;
		}

		public override int FieldCount => this.rows == null ? base.FieldCount : this.fieldCount;
		public override object this[int i] => this.rows == null ? base[i] : this.currentRow.values[i];
		public override object this[string name] => this.rows == null ? base[name] : this.currentRow.values[this.ordinalByFieldName[name]];
		public override bool IsClosed => this.rows == null ? base.IsClosed : this.closed;
		public override int RecordsAffected => this.rows == null ? base.RecordsAffected : this.recordsAffected;

		public override void Close()
		{
			if (this.rows == null)
			{
				base.Close();
			}

			this.closed = true;
			this.rows = null;
		}
	}
}
