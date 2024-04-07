using SharpOrm.Builder.DataTranslation;
using SharpOrm.Builder.DataTranslation.Reader;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

namespace SharpOrm.Collections
{
    public class DbObjectEnumerable<T> : IEnumerable<T>
    {
        private readonly TranslationRegistry translation;
        private readonly CancellationToken token;
        private readonly DbDataReader reader;
        internal IFkQueue fkQueue;

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
            return MappedObject.Create(reader, typeof(T), this.fkQueue ?? new ObjIdFkQueue(), translation);
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
