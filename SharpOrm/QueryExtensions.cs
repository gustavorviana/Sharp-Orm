using SharpOrm.Connection;
using SharpOrm.Errors;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOrm
{
    /// <summary>
    /// Extension methods for Query&lt;T&gt; to provide additional functionality.
    /// </summary>
    public static class QueryExtensions
    {
        #region Pagination

        /// <summary>
        /// Skips the specified number of rows.
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <param name="count">Number of rows to skip.</param>
        /// <returns>The current query instance.</returns>
        public static Query<T> Skip<T>(this Query<T> query, int count)
        {
            query.Offset = count;
            return query;
        }

        /// <summary>
        /// Takes only the specified number of rows.
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <param name="count">Number of rows to take.</param>
        /// <returns>The current query instance.</returns>
        public static Query<T> Take<T>(this Query<T> query, int count)
        {
            query.Limit = count;
            return query;
        }

        /// <summary>
        /// Applies pagination using page number and page size.
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <param name="page">Page number (1-based).</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>The current query instance.</returns>
        public static Query<T> Page<T>(this Query<T> query, int page, int pageSize)
        {
            if (page < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(page));
            if (pageSize < 1)
                throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

            query.Offset = (page - 1) * pageSize;
            query.Limit = pageSize;
            return query;
        }

        #endregion

        #region Soft Delete Methods

        /// <summary>
        /// Includes soft deleted items in the query results.
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <returns>The current query instance.</returns>
        public static Query<T> WithTrashed<T>(this Query<T> query)
        {
            query.Trashed = Trashed.With;
            return query;
        }

        /// <summary>
        /// Excludes soft deleted items from the query results (default behavior).
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <returns>The current query instance.</returns>
        public static Query<T> WithoutTrashed<T>(this Query<T> query)
        {
            query.Trashed = Trashed.Except;
            return query;
        }

        /// <summary>
        /// Returns only soft deleted items.
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <returns>The current query instance.</returns>
        public static Query<T> OnlyTrashed<T>(this Query<T> query)
        {
            query.Trashed = Trashed.Only;
            return query;
        }

        #endregion

        #region CRUD Shortcuts

        /// <summary>
        /// Finds a record by its primary key value.
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <param name="id">The primary key value.</param>
        /// <returns>The entity if found, null otherwise.</returns>
        public static T FindById<T>(this Query<T> query, object id)
        {
            var keys = query.TableInfo.GetPrimaryKeys();
            if (keys.Length == 0)
                throw new DatabaseException(Messages.MissingPrimaryKey);

            if (keys.Length > 1)
                throw new InvalidOperationException("FindById does not support composite primary keys. Use FindByPk instead.");

            using (var newQuery = new Query<T>(query.Manager))
            {
                newQuery.Token = query.Token;
                newQuery.CommandTimeout = query.CommandTimeout;
                newQuery.WherePk(id);
                return newQuery.FirstOrDefault();
            }
        }

        /// <summary>
        /// Asynchronously finds a record by its primary key value.
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <param name="id">The primary key value.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The entity if found, null otherwise.</returns>
        public static async Task<T> FindByIdAsync<T>(this Query<T> query, object id, CancellationToken token = default)
        {
            var keys = query.TableInfo.GetPrimaryKeys();
            if (keys.Length == 0)
                throw new DatabaseException(Messages.MissingPrimaryKey);

            if (keys.Length > 1)
                throw new InvalidOperationException("FindByIdAsync does not support composite primary keys. Use FindByPkAsync instead.");

            using (var newQuery = new Query<T>(query.Manager))
            {
                newQuery.Token = token;
                newQuery.CommandTimeout = query.CommandTimeout;
                newQuery.WherePk(id);
                return await newQuery.FirstOrDefaultAsync(token);
            }
        }

        /// <summary>
        /// Updates a record by its primary key value.
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <param name="id">The primary key value.</param>
        /// <param name="value">The entity with updated values.</param>
        /// <returns>Number of affected rows.</returns>
        public static int UpdateById<T>(this Query<T> query, object id, T value)
        {
            var keys = query.TableInfo.GetPrimaryKeys();
            if (keys.Length == 0)
                throw new DatabaseException(Messages.MissingPrimaryKey);

            if (keys.Length > 1)
                throw new InvalidOperationException("UpdateById does not support composite primary keys. Use UpdateByPk instead.");

            using (var newQuery = new Query<T>(query.Manager))
            {
                newQuery.Token = query.Token;
                newQuery.CommandTimeout = query.CommandTimeout;
                newQuery.WherePk(id);
                return newQuery.Update(value);
            }
        }

        /// <summary>
        /// Asynchronously updates a record by its primary key value.
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <param name="id">The primary key value.</param>
        /// <param name="value">The entity with updated values.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of affected rows.</returns>
        public static async Task<int> UpdateByIdAsync<T>(this Query<T> query, object id, T value, CancellationToken token = default)
        {
            var keys = query.TableInfo.GetPrimaryKeys();
            if (keys.Length == 0)
                throw new DatabaseException(Messages.MissingPrimaryKey);

            if (keys.Length > 1)
                throw new InvalidOperationException("UpdateByIdAsync does not support composite primary keys. Use UpdateByPkAsync instead.");

            using (var newQuery = new Query<T>(query.Manager))
            {
                newQuery.Token = token;
                newQuery.CommandTimeout = query.CommandTimeout;
                newQuery.WherePk(id);
                return await newQuery.UpdateAsync(value, token);
            }
        }

        /// <summary>
        /// Deletes a record by its primary key value.
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <param name="id">The primary key value.</param>
        /// <returns>Number of affected rows.</returns>
        public static int DeleteById<T>(this Query<T> query, object id)
        {
            var keys = query.TableInfo.GetPrimaryKeys();
            if (keys.Length == 0)
                throw new DatabaseException(Messages.MissingPrimaryKey);

            if (keys.Length > 1)
                throw new InvalidOperationException("DeleteById does not support composite primary keys. Use DeleteByPk instead.");

            using (var newQuery = new Query<T>(query.Manager))
            {
                newQuery.Token = query.Token;
                newQuery.CommandTimeout = query.CommandTimeout;
                newQuery.WherePk(id);
                return newQuery.Delete();
            }
        }

        /// <summary>
        /// Asynchronously deletes a record by its primary key value.
        /// </summary>
        /// <param name="query">The query instance.</param>
        /// <param name="id">The primary key value.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of affected rows.</returns>
        public static async Task<int> DeleteByIdAsync<T>(this Query<T> query, object id, CancellationToken token = default)
        {
            var keys = query.TableInfo.GetPrimaryKeys();
            if (keys.Length == 0)
                throw new DatabaseException(Messages.MissingPrimaryKey);

            if (keys.Length > 1)
                throw new InvalidOperationException("DeleteByIdAsync does not support composite primary keys. Use DeleteByPkAsync instead.");

            using (var newQuery = new Query<T>(query.Manager))
            {
                newQuery.Token = token;
                newQuery.CommandTimeout = query.CommandTimeout;
                newQuery.WherePk(id);
                return await newQuery.DeleteAsync(token);
            }
        }

        #endregion
    }
}
