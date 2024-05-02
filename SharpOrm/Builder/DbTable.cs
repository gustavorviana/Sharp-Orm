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
        private TableGrammar grammar;
        private bool disposed;

        /// <summary>
        /// Table connection manager.
        /// </summary>
        public ConnectionManager Manager { get; }
        /// <summary>
        /// Table name.
        /// </summary>
        public DbName Name => grammar.Name;
        #endregion


        /// <summary>
        /// Creates a temporary table based on a schema.
        /// </summary>
        /// <param name="schema">Schema to be used for creating the table.</param>
        /// <returns></returns>
        public static DbTable CreateTemp(TableSchema schema, ConnectionCreator creator)
        {
            return Create(schema, new ConnectionManager(creator));
        }

        /// <summary>
        /// Creates a temporary table based on a schema.
        /// </summary>
        /// <param name="schema">Schema to be used for creating the table.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        public static DbTable CreateTemp(TableSchema schema, ConnectionManager manager = null)
        {
            schema.Temporary = true;
            return Create(schema, manager);
        }

        /// <summary>
        /// Creates a temporary table based on the provided columns.
        /// </summary>
        /// <param name="columns">Columns that the table should contain.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        public static DbTable CreateTemp(string name, TableColumnCollection columns, ConnectionCreator creator)
        {
            return Create(new TableSchema(name, columns), new ConnectionManager(creator));
        }

        /// <summary>
        /// Creates a temporary table based on the provided columns.
        /// </summary>
        /// <param name="columns">Columns that the table should contain.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        public static DbTable CreateTemp(string name, TableColumnCollection columns, ConnectionManager manager = null)
        {
            return Create(new TableSchema(name, columns), manager);
        }

        /// <summary>
        /// Creates a table based on a schema.
        /// </summary>
        /// <param name="schema">Schema to be used for creating the table.</param>
        /// <returns></returns>
        public static DbTable Create(TableSchema schema, ConnectionCreator creator)
        {
            return Create(schema, new ConnectionManager(creator));
        }

        /// <summary>
        /// Creates a table based on a schema.
        /// </summary>
        /// <param name="schema">Schema to be used for creating the table.</param>
        /// <param name="manager">Managed connection used to create the table.</param>
        /// <returns></returns>
        public static DbTable Create(TableSchema schema, ConnectionManager manager = null)
        {
            if (manager is null)
                manager = new ConnectionManager();

            var grammar = manager.Config.NewTableGrammar(schema.Clone());
            using (var cmd = manager.GetCommand().SetExpression(grammar.Create()))
                cmd.ExecuteNonQuery();

            return new DbTable(grammar, manager);
        }

        /// <summary>
        /// Opens an existing table.
        /// </summary>
        /// <param name="schema">Schema (Only name and if it's temporary) of the table.</param>
        /// <returns></returns>
        /// <exception cref="DatabaseException"></exception>
        public static DbTable Open(TableSchema schema, ConnectionCreator creator)
        {
            return Open(schema, new ConnectionManager(creator));
        }

        /// <summary>
        /// Opens an existing table.
        /// </summary>
        /// <param name="schema">Schema (Only name and if it's temporary) of the table.</param>
        /// <param name="manager">Managed connection used to open the table.</param>
        /// <returns></returns>
        /// <exception cref="DatabaseException"></exception>
        public static DbTable Open(TableSchema schema, ConnectionManager manager)
        {
            if (manager is null)
                manager = new ConnectionManager();

            var grammar = manager.Config.NewTableGrammar(schema.Clone());
            if (!Exists(schema, manager))
                throw new DatabaseException($"The table '{grammar.Name}' was not found.");

            return new DbTable(grammar, manager);
        }

        private DbTable(TableGrammar grammar, ConnectionManager manager)
        {
            ConfigureManager(manager, grammar.Schema);
            manager.CloseByEndOperation();
            this.Manager = manager;
            this.grammar = grammar;
        }

        /// <summary>
        /// Signals whether the table exists.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Exists(TableSchema schema, ConnectionCreator creator)
        {
            return Exists(schema, new ConnectionManager(creator));
        }

        /// <summary>
        /// Signals whether the table exists.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="manager"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Exists(TableSchema schema, ConnectionManager manager)
        {
            if (manager is null)
                throw new ArgumentNullException(nameof(manager));

            ConfigureManager(manager, schema);

            try
            {
                using (var cmd = manager.GetCommand().SetExpression(manager.Config.NewTableGrammar(schema).Count()))
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
        public void DropTable()
        {
            try
            {
                using (var cmd = Manager.GetCommand().SetExpression(grammar.Drop()))
                    cmd.ExecuteNonQuery();
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

        private static void ConfigureManager(ConnectionManager manager, TableSchema schema)
        {
            if (manager.Transaction is null && schema.Temporary)
                manager.Management = ConnectionManagement.LeaveOpen;
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            try
            {
                if (grammar.Schema.Temporary)
                    DropTable();
            }
            catch { }

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
