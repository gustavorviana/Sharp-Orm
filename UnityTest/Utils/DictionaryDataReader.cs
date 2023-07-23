using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace UnityTest.Utils
{
    public class DictionaryDataReader : DbDataReader
    {
        private readonly List<Dictionary<string, object>> data;
        private int currentIndex = -1;

        public DictionaryDataReader(List<Dictionary<string, object>> data)
        {
            this.data = data;
        }

        public override bool HasRows => data.Count > 0;
        public override object this[string name] => data[currentIndex][name];

        public override object this[int i] => GetValue(i);

        public override int Depth => 0;

        public override bool IsClosed => currentIndex >= data.Count || currentIndex < 0;

        public override int RecordsAffected => -1;

        public override int FieldCount => data.Count > 0 ? data[0].Count : 0;

        public override void Close()
        {
            currentIndex = data.Count;
        }

        public override bool NextResult()
        {
            return false;
        }

        public override bool Read()
        {
            if (currentIndex < data.Count - 1)
            {
                currentIndex++;
                return true;
            }
            return false;
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
            return data[currentIndex].Keys.ElementAt(i);
        }

        public override int GetOrdinal(string name)
        {
            int index = 0;
            foreach (var key in data[currentIndex].Keys)
            {
                if (key.Equals(name, StringComparison.OrdinalIgnoreCase))
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
            foreach (var dictionary in data)
                yield return dictionary;
        }

        public override object GetValue(int ordinal)
        {
            return data[currentIndex].Values.ElementAt(ordinal) ?? DBNull.Value;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            data.Clear();
        }
    }
}
