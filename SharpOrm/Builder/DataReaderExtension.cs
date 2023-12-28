using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace SharpOrm.Builder
{
    public static class DataReaderExtension
    {
        /// <summary>
        /// Get row of current reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Row ReadRow(this DbDataReader reader, IQueryConfig config)
        {
            Cell[] cells = new Cell[reader.FieldCount];

            for (int i = 0; i < cells.Length; i++)
                cells[i] = GetCell(reader, config, i);

            return new Row(cells);
        }

        /// <summary>
        /// Get Cell by column index.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Cell GetCell(this DbDataReader reader, IQueryConfig config, int index)
        {
            if (index < 0 || index > reader.FieldCount)
                throw new ArgumentOutOfRangeException();

            return new Cell(reader.GetName(index), LoadDbValue(config, reader[index]));
        }

        internal static object LoadDbValue(IQueryConfig config, object obj)
        {
            if (obj is DBNull)
                return null;

            if (obj is DateTime date && config.DateKind == DateTimeKind.Utc)
                return date.FromDatabase(config);

            return obj;
        }
    }
}
