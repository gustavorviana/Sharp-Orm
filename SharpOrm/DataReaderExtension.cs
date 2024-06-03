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
        /// Get row of current reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Row ReadRow(this DbDataReader reader, TranslationRegistry translation = null)
        {
            if (translation == null)
                translation = TranslationRegistry.Default;

            Cell[] cells = new Cell[reader.FieldCount];

            for (int i = 0; i < cells.Length; i++)
                cells[i] = reader.GetCell(translation, i);

            return new Row(cells);
        }

        /// <summary>
        /// Get Cell by column index.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Cell GetCell(this DbDataReader reader, TranslationRegistry translation, int index)
        {
            if (index < 0 || index > reader.FieldCount)
                throw new ArgumentOutOfRangeException();

            return new Cell(reader.GetName(index), translation.FromSql(reader[index]));
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
