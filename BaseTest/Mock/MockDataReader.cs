﻿using Bogus;
using SharpOrm;
using System.Collections;
using System.Data.Common;

namespace BaseTest.Mock
{
    public class MockDataReader : DbDataReader
    {
        private MockCommand? command;
        private readonly CancellationTokenSource tokenSrc = new();
        private readonly Func<int, Row> rowsCall;
        public int ReadDelay { get; set; }
        public int Size { get; }

        private int currentIndex = -1;

        public MockDataReader(params Cell[] cells) : this(i => cells.Length > 0 ? new Row(cells) : null!, cells.Length > 0 ? 1 : 0)
        {

        }

        public MockDataReader(Func<int, Row> rowsCall, int size)
        {
            this.rowsCall = rowsCall;
            this.Size = size;

            if (size > 0)
                this.currentRow = this.rowsCall(0);
        }

        public static MockDataReader FromFaker<T>(Faker<T> faker, int items) where T : class
        {
            return new MockDataReader(i => Row.Parse(faker.Generate()), items);
        }

        public void Cancel()
        {
            this.tokenSrc.Cancel();
        }

        public MockDataReader SetCommand(MockCommand cmd)
        {
            this.command = cmd;
            this.command.OnCancel += (sender, e) => this.Cancel();
            return this;
        }

        private Row? currentRow = null;

        public override bool HasRows => this.Size > 0;
        public override object this[string name] => this.currentRow![name];

        public override object this[int i] => GetValue(i);

        public override int Depth => 1;

        private bool closed = false;
        public override bool IsClosed => this.closed;

        public override int RecordsAffected => -1;

        public override int FieldCount => this.currentRow?.Count ?? 0;

        public override void Close()
        {
            this.closed = true;
            currentIndex = this.Size;
        }

        public override bool NextResult()
        {
            return false;
        }

        public override bool Read()
        {
            this.WaitDelay();
            if (command?.Cancelled == true) return false;

            if (currentIndex < this.Size - 1)
            {
                currentIndex++;
                this.currentRow = this.rowsCall(this.currentIndex);

                return true;
            }
            return false;
        }

        private void WaitDelay()
        {
            try
            {
                if (this.ReadDelay > 0)
                    Task.Delay(this.ReadDelay).Wait(this.tokenSrc.Token);
            }
            catch (OperationCanceledException) { }
        }

        public override bool GetBoolean(int i)
        {
            return Convert.ToBoolean(GetValue(i));
        }

        public override byte GetByte(int i)
        {
            return Convert.ToByte(GetValue(i));
        }

        public override long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
        {
            byte[] byteArray = (byte[])GetValue(i);
            long bytesRead = Math.Min(byteArray.Length - fieldOffset, length);
            Array.Copy(byteArray, fieldOffset, buffer!, bufferoffset, bytesRead);
            return bytesRead;
        }

        public override char GetChar(int i)
        {
            return Convert.ToChar(GetValue(i));
        }

        public override long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
        {
            char[] charArray = GetValue(i).ToString()!.ToCharArray();
            long charCount = Math.Min(charArray.Length - fieldoffset, length);
            Array.Copy(charArray, fieldoffset, buffer!, bufferoffset, charCount);
            return charCount;
        }

        public override string GetDataTypeName(int i)
        {
            return GetValue(i).GetType().Name;
        }

        public override DateTime GetDateTime(int i)
        {
            return Convert.ToDateTime(GetValue(i));
        }

        public override decimal GetDecimal(int i)
        {
            return Convert.ToDecimal(GetValue(i));
        }

        public override double GetDouble(int i)
        {
            return Convert.ToDouble(GetValue(i));
        }

        public override Type GetFieldType(int i)
        {
            return GetValue(i)?.GetType()!;
        }

        public override float GetFloat(int i)
        {
            return Convert.ToSingle(GetValue(i));
        }

        public override Guid GetGuid(int i)
        {
            return Guid.Parse(GetValue(i).ToString()!);
        }

        public override short GetInt16(int i)
        {
            return Convert.ToInt16(GetValue(i));
        }

        public override int GetInt32(int i)
        {
            return Convert.ToInt32(GetValue(i));
        }

        public override long GetInt64(int i)
        {
            return Convert.ToInt64(GetValue(i));
        }

        public override string GetName(int i)
        {
            return this.currentRow?[i].Name!;
        }

        public override int GetOrdinal(string name)
        {
            int index = 0;
            foreach (var row in this.currentRow!)
            {
                if (row.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return index;

                index++;
            }

            return -1;
        }

        public override string GetString(int i)
        {
            return GetValue(i).ToString()!;
        }

        public override int GetValues(object[] values)
        {
            int copyLength = Math.Min(FieldCount, values.Length);
            for (int i = 0; i < copyLength; i++)
                values[i] = GetValue(i);

            return copyLength;
        }

        public override bool IsDBNull(int i)
        {
            return GetValue(i) == null || GetValue(i) == DBNull.Value;
        }

        public override IEnumerator GetEnumerator()
        {
            foreach (var dictionary in this.currentRow!)
                yield return dictionary;
        }

        public override object GetValue(int ordinal)
        {
            return this.currentRow![ordinal].Value ?? DBNull.Value;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
