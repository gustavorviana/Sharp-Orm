﻿using SharpOrm.Connection;
using SharpOrm.Errors;
using System;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a table in the database.
    /// </summary>
    public class DbTable : IDisposable
    {
        #region Fields/Properties
        private readonly TableGrammar grammar;
        private bool disposed;
        public static bool RandomNameForTempTable { get; set; }

        /// <summary>
        /// Table connection manager.
        /// </summary>
        public ConnectionManager Manager { get; }
        private bool isLocalManager = false;

        /// <summary>
        /// Table name.
        /// </summary>
        public DbName Name => grammar.Name;
        private bool dropped = false;
        #endregion

        public static DbTable Create(string name, bool temporary, Query queryBase, ConnectionManager manager = null)
        {
            return Create(new TableSchema(name, queryBase) { Temporary = temporary }, manager ?? queryBase.Manager);
        }

        public static DbTable Create(string name, bool temporary, Column[] columns, string basedTable, ConnectionManager manager = null)
        {
            var query = Query.ReadOnly(basedTable, manager?.Config).Select(columns);
            query.Limit = 0;

            return Create(new TableSchema(name, query) { Temporary = temporary }, manager);
        }

        /// <summary>
        /// Creates a table based on the provided columns.
        /// </summary>
        /// <param name="columns">Columns that the table should contain.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        public static DbTable Create(string name, bool temporary, TableColumnCollection columns, ConnectionManager manager = null)
        {
            return Create(new TableSchema(name, columns) { Temporary = temporary }, manager);
        }

        /// <summary>
        /// Creates a table based on a schema.
        /// </summary>
        /// <param name="schema">Schema to be used for creating the table.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        public static DbTable Create(TableSchema schema, ConnectionManager manager = null)
        {
            bool IsLocalManager = manager == null;
            if (manager is null)
                manager = new ConnectionManager() { Management = ConnectionManagement.CloseOnManagerDispose };

            ValidateConnectionManager(schema, manager);

            var clone = schema.Clone();
            if (RandomNameForTempTable)
                clone.Name = string.Concat(Guid.NewGuid().ToString("N"), "_", clone.Name);

            if (manager.Transaction is null && manager.Management == ConnectionManagement.CloseOnEndOperation)
                manager.Management = ConnectionManagement.CloseOnDispose;

            var grammar = manager.Config.NewTableGrammar(clone);
            using (var cmd = manager.CreateCommand().SetExpression(grammar.Create()))
                cmd.ExecuteNonQuery();

            return new DbTable(grammar, manager) { isLocalManager = IsLocalManager};
        }

        /// <summary>
        /// Opens an existing non temporary table;
        /// </summary>
        /// <param name="name"></param>
        /// <param name="manager"></param>
        /// <exception cref="DatabaseException"></exception>
        public DbTable(string name, ConnectionManager manager = null)
        {
            this.Manager = manager ?? new ConnectionManager() { Management = ConnectionManagement.CloseOnManagerDispose };
            this.grammar = manager.Config.NewTableGrammar(new TableSchema(name) { Temporary = false });
            this.isLocalManager = manager == null;

            if (!this.Exists())
                throw new DatabaseException($"The table '{grammar.Name}' was not found.");
        }

        private DbTable(TableGrammar grammar, ConnectionManager manager)
        {
            this.Manager = manager;
            this.grammar = grammar;
        }

        /// <summary>
        /// Checks if table exists.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Exists()
        {
            return Exists(this.grammar, this.Manager);
        }

        /// <summary>
        /// Checks if table exists.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Exists(string name, bool isTemp = false, ConnectionManager manager = null)
        {
            if (manager is null)
                throw new ArgumentNullException(nameof(manager));

            return Exists(
                manager.Config.NewTableGrammar(new TableSchema(name) { Temporary = isTemp }),
                manager
            );
        }

        /// <summary>
        /// Checks if table exists.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static bool Exists(TableGrammar grammar, ConnectionManager manager)
        {
            try
            {
                using (var cmd = manager.CreateCommand().SetExpression(grammar.Exists()))
                    return cmd.ExecuteScalar<int>() > 0;
            }
            finally
            {
                manager.CloseByEndOperation();
            }
        }

        /// <summary>
        /// Deletes the table from the database.
        /// </summary>
        public void Drop()
        {
            try
            {
                using (var cmd = Manager.CreateCommand().SetExpression(grammar.Drop()))
                    cmd.ExecuteNonQuery();

                this.dropped = true;
            }
            finally
            {
                if (Manager.CanClose)
                    Manager.Connection.Close();
            }
        }

        /// <summary>
        /// Retrieves a query object for the table.
        /// </summary>
        /// <returns></returns>
        public Query GetQuery()
        {
            return new Query(Name, Manager);
        }

        /// <summary>
        /// Retrieves a query object for the table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Query GetQuery<T>() where T : new()
        {
            return new Query<T>(Name, Manager);
        }

        private static void ValidateConnectionManager(TableSchema schema, ConnectionManager manager)
        {
            if (schema.Temporary && manager.Management != ConnectionManagement.LeaveOpen && manager.Management != ConnectionManagement.CloseOnManagerDispose)
                manager.Management = ConnectionManagement.LeaveOpen;
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            try
            {
                if (grammar.Schema.Temporary && !this.dropped)
                    Drop();
            }
            catch { }

            if (disposing && this.isLocalManager)
                this.Manager.Dispose();

            disposed = true;
        }

        ~DbTable()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            if (disposed) return;

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
