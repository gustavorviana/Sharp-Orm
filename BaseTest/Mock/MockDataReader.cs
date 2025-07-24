using Bogus;
using SharpOrm;
using System.Collections;
using System.Data.Common;

namespace BaseTest.Mock
{
    public class MockDataReader : DbDataReader
    {
        private MockCommand? command;
        private readonly CancellationTokenSource tokenSrc = new();
        private readonly Func<int, Row> _rowsCall;
        public int ReadDelay { get; set; }
        public int Size { get; }

        private int currentIndex = -1;

        public MockDataReader(params Cell[] cells) : this(i => cells.Length > 0 ? new Row(cells) : null!, cells.Length > 0 ? 1 : 0)
        {

        }

        public MockDataReader(Func<int, Row> rowsCall, int size)
        {
            _rowsCall = rowsCall;
            Size = size;

            if (size > 0)
                currentRow = _rowsCall(0);
        }

        public static MockDataReader FromFaker<T>(Faker<T> faker, int items) where T : class
        {
            return new MockDataReader(i => Row.Parse(faker.Generate()), items);
        }

        public void Cancel()
        {
            tokenSrc.Cancel();
        }

        public MockDataReader SetCommand(MockCommand cmd)
        {
            command = cmd;
            command.OnCancel += (sender, e) => Cancel();
            return this;
        }

        private Row? currentRow = null;

        public override bool HasRows => Size > 0;
        public override object this[string name] => currentRow![name];

        public override object this[int i] => GetValue(i);

        public override int Depth => 1;

        private bool closed = false;
        public override bool IsClosed => closed;

        private int _recordsAffected = -1;
        public override int RecordsAffected => _recordsAffected;

        public override int FieldCount => currentRow?.Count ?? 0;

        public MockDataReader SetRecordsAffected(int value)
        {
            _recordsAffected = value;
            return this;
        }

        public override void Close()
        {
            closed = true;
            currentIndex = Size;
        }

        public override bool NextResult()
        {
            return false;
        }

        public override bool Read()
        {
            WaitDelay();
            if (command?.Cancelled == true) return false;

            if (currentIndex < Size - 1)
            {
                currentIndex++;
                currentRow = _rowsCall(currentIndex);

                return true;
            }
            return false;
        }

        private void WaitDelay()
        {
            try
            {
                if (ReadDelay > 0)
                    Task.Delay(ReadDelay).Wait(tokenSrc.Token);
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
            return currentRow?[i].Name!;
        }

        public override int GetOrdinal(string name)
        {
            int index = 0;
            foreach (var row in currentRow!)
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
            foreach (var dictionary in currentRow!)
                yield return dictionary;
        }

        public override object GetValue(int ordinal)
        {
            return currentRow![ordinal].Value ?? DBNull.Value;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
