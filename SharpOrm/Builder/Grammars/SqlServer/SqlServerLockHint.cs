using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    /// <summary>
    /// SQL Server table hints that influence lock granularity.
    /// Only one option should be used at a time.
    /// </summary>
    public enum SqlServerLockHint
    {
        /// <summary>
        /// No lock-granularity hint is emitted.
        /// SQL Server chooses row/page/table locks automatically.
        /// </summary>
        None,

        /// <summary>
        /// ROWLOCK.
        /// Forces row-level locks (when possible).
        /// Increases concurrency but may increase lock overhead.
        /// </summary>
        RowLock,

        /// <summary>
        /// PAGLOCK.
        /// Forces page-level locks (when possible).
        /// Reduces lock count but reduces concurrency.
        /// </summary>
        PagLock,

        /// <summary>
        /// TABLOCK.
        /// Forces a table-level lock (shared/update depending on operation).
        /// Reduces lock overhead but increases blocking.
        /// </summary>
        TabLock,

        /// <summary>
        /// TABLOCKX.
        /// Forces an exclusive table-level lock.
        /// Blocks all reads and writes from other sessions while held.
        /// </summary>
        TabLockX
    }
}
