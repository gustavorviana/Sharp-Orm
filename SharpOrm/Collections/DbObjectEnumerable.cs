using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using SharpOrm.Builder.DataTranslation.Reader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace SharpOrm.Collections
{
    internal class DbObjectEnumerable<T> : IEnumerable<T>, IFkQueue
    {
        private readonly TranslationRegistry translation;
        private readonly CancellationToken token;
        private readonly DbDataReader reader;

        public DbObjectEnumerable(Query query) : this(query.Info.Config.Translation, query.ExecuteReader(), query.Token)
        {

        }

        public DbObjectEnumerable(TranslationRegistry translation, DbDataReader reader, CancellationToken token)
        {
            this.translation = translation;
            this.reader = reader;
            this.token = token;
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(reader, this.CreateMappedObj(), token);
        IEnumerator IEnumerable.GetEnumerator() => new DbObjectEnumerator(reader, this.CreateMappedObj(), token);

        private IMappedObject CreateMappedObj()
        {
            return MappedObject.Create(reader, typeof(T), this, translation);
        }

        void IFkQueue.EnqueueForeign(object owner, object fkValue, ColumnInfo column)
        {
            if (fkValue is DBNull || fkValue is null)
                return;

            var fkTable = new TableInfo(column.Type);
            object value = fkTable.CreateInstance();
            fkTable.Columns.FirstOrDefault(c => c.Key)?.Set(value, fkValue);
            column.SetRaw(owner, value);
        }

        private class Enumerator : DbObjectEnumerator, IEnumerator<T>
        {
            public Enumerator(DbDataReader reader, IMappedObject map, CancellationToken token) : base(reader, map, token)
            {
            }

            public new T Current => (T)base.Current;
        }
    }
}
