using SharpOrm;
using System;
using System.Collections;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace UnityTest.Utils.Mock
{
    public class MockDataReader : DbDataReader
    {
        public readonly Row[] rows;
        private int currentIndex = -1;
        public int ReadDelay { get; set; }

        public MockDataReader(Row[] rows)
        {
            this.rows = rows;
        }

        public CancellationToken Token { get; set; }

        public override bool HasRows => rows.Length > 0;
        public override object this[string name] => rows[currentIndex][name];

        public override object this[int i] => GetValue(i);

        public override int Depth => 0;

        public override bool IsClosed => currentIndex >= rows.Length || currentIndex < 0;

        public override int RecordsAffected => -1;

        public override int FieldCount => rows.Length > 0 ? rows[0].Count : 0;

        public override void Close()
        {
            currentIndex = rows.Length;
        }

        public override bool NextResult()
        {
            return false;
        }

        public override bool Read()
        {
            if (currentIndex < rows.Length - 1)
            {
                currentIndex++;
                this.WaitDelay();

                return true;
            }
            return false;
        }

        private void WaitDelay()
        {
            try
            {
                if (this.ReadDelay > 0)
                    Task.Delay(this.ReadDelay).Wait(this.Token);
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

        public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            byte[] byteArray = (byte[])GetValue(i);
            long bytesRead = Math.Min(byteArray.Length - fieldOffset, length);
            Array.Copy(byteArray, fieldOffset, buffer, bufferoffset, bytesRead);
            return bytesRead;
        }

        public override char GetChar(int i)
        {
            return Convert.ToChar(GetValue(i));
        }

        public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            char[] charArray = GetValue(i).ToString().ToCharArray();
            long charCount = Math.Min(charArray.Length - fieldoffset, length);
            Array.Copy(charArray, fieldoffset, buffer, bufferoffset, charCount);
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
            return GetValue(i)?.GetType();
        }

        public override float GetFloat(int i)
        {
            return Convert.ToSingle(GetValue(i));
        }

        public override Guid GetGuid(int i)
        {
            return Guid.Parse(GetValue(i).ToString());
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
            return rows[currentIndex][i].Name;
        }

        public override int GetOrdinal(string name)
        {
            int index = 0;
            foreach (var row in rows[currentIndex])
            {
                if (row.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return index;

                index++;
            }
            throw new IndexOutOfRangeException("Column not found: " + name);
        }

        public override string GetString(int i)
        {
            return GetValue(i).ToString();
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
            foreach (var dictionary in rows)
                yield return dictionary;
        }

        public override object GetValue(int ordinal)
        {
            return rows[currentIndex][ordinal].Value ?? DBNull.Value;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
