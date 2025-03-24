using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using System;
using System.Data;
using System.Data.Common;

namespace SharpOrm
{
    public static class DataReaderExtension
    {
        /// <summary>
        /// Get row of current record.
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public static Row ReadRow(this IDataRecord record, TranslationRegistry translation = null)
        {
            if (translation == null)
                translation = TranslationRegistry.Default;

            Cell[] cells = new Cell[record.FieldCount];

            for (int i = 0; i < cells.Length; i++)
                cells[i] = record.GetCell(translation, i);

            return new Row(cells);
        }

        /// <summary>
        /// Get Cell by column index.
        /// </summary>
        /// <param name="record"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Cell GetCell(this IDataRecord record, TranslationRegistry translation, int index)
        {
            if (index < 0 || index > record.FieldCount)
                throw new ArgumentOutOfRangeException();

            return new Cell(record.GetName(index), translation.FromSql(record[index]));
        }

        internal static T GetValue<T>(this IDataReader reader, ISqlTranslation translation, Type expectedType)
        {
            if (reader.IsDBNull(0)) return default;
            else return (T)translation.FromSqlValue(reader.GetValue(0), expectedType);
        }

        internal static bool CanClose(this DbConnection connection, ConnectionManagement management)
        {
            return management == ConnectionManagement.CloseOnEndOperation && connection.IsOpen();
        }

        internal static bool IsOpen(this DbConnection connection)
        {
            try
            {
                return connection.State == ConnectionState.Open || connection.State == ConnectionState.Connecting || connection.State == ConnectionState.Executing;
            }
            catch
            {
                return false;
            }
        }
    }
}
