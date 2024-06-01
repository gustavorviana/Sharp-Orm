using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.DataTranslation;
using SharpOrm.DataTranslation.Reader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

namespace SharpOrm.Collections
{
    public class DbCommandEnumerable<T> : IEnumerable<T>
    {
        private readonly TranslationRegistry translation;
        private readonly CancellationToken token;
        private readonly ConnectionManagement management;
        private readonly DbCommand command;
        internal IFkQueue fkQueue;
        private bool hasFirstRun;

        public bool LeaveOpen { get; set; }

        public DbCommandEnumerable(TranslationRegistry translation, DbCommand command, CancellationToken token, ConnectionManagement management)
        {
            Grammar.QueryLogger?.Invoke(command.CommandText);
            this.translation = translation;
            this.management = management;
            this.command = command;
            this.token = token;
        }

        public IEnumerator<T> GetEnumerator()
        {
            this.CheckRun();
            var reader = command.ExecuteReader();
            return RegisterDispose(new DbObjectEnumerator<T>(reader, this.CreateMappedObj(reader), token));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            this.CheckRun();
            var reader = command.ExecuteReader();
            return RegisterDispose(new DbObjectEnumerator(reader, this.CreateMappedObj(reader), token, this.LeaveOpen));
        }

        private void CheckRun()
        {
            if (this.hasFirstRun)
                throw new InvalidOperationException("IEnumerable can be executed only once.");

            this.hasFirstRun = true;
        }

        private IMappedObject CreateMappedObj(DbDataReader reader)
        {
            return MappedObject.Create(reader, typeof(T), this.fkQueue, translation);
        }

        private K RegisterDispose<K>(K instance) where K : DbObjectEnumerator
        {
            instance.Disposed += (sender, e) =>
            {
                try
                {
                    if (this.management != ConnectionManagement.LeaveOpen && this.management != ConnectionManagement.CloseOnManagerDispose && this.command.CanCloseConnection(this.management))
                        this.command.Connection.Close();

                }
                catch
                { }

                try { this.command.Dispose(); } catch { }
            };

            return instance;
        }
    }
}
