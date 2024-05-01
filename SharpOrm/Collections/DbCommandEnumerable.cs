﻿using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using SharpOrm.Builder.DataTranslation.Reader;
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

        public DbCommandEnumerable(TranslationRegistry translation, DbCommand command, CancellationToken token, ConnectionManagement management)
        {
            this.translation = translation;
            this.management = management;
            this.command = command;
            this.token = token;
        }

        public IEnumerator<T> GetEnumerator()
        {
            this.CheckRun();
            var reader = command.ExecuteReader();
            return RegisterDispose(new Enumerator(reader, this.CreateMappedObj(reader), token));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            this.CheckRun();
            var reader = command.ExecuteReader();
            return RegisterDispose(new DbObjectEnumerator(reader, this.CreateMappedObj(reader), token));
        }

        private void CheckRun()
        {
            if (this.hasFirstRun)
                throw new InvalidOperationException("IEnumerable can be executed only once.");

            this.hasFirstRun = true;
        }

        private IMappedObject CreateMappedObj(DbDataReader reader)
        {
            return MappedObject.Create(reader, typeof(T), this.fkQueue ?? new ObjIdFkQueue(), translation);
        }

        private K RegisterDispose<K>(K instance) where K : DbObjectEnumerator
        {
            instance.Disposed += (sender, e) =>
            {
                this.command.Dispose();
                if (this.command.CanCloseConnection(this.management))
                    this.command.Connection.Close();
            };

            return instance;
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