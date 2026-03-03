using System;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    /// <summary>
    /// SQL Server table hints that influence concurrency behavior around locked rows.
    /// These can be combined.
    /// </summary>
    [Flags]
    public enum SqlServerConcurrencyHint
    {
        /// <summary>
        /// No concurrency hints are emitted.
        /// </summary>
        None = 0,

        /// <summary>
        /// READPAST.
        /// Skips rows locked by other transactions instead of waiting.
        /// Common for queue/worker patterns to avoid contention.
        /// Can cause "missed" rows if everything is locked.
        /// </summary>
        ReadPast = 1,

        /// <summary>
        /// UPDLOCK.
        /// Takes update locks while reading rows.
        /// Helps prevent deadlocks / race conditions when you plan to update the same rows.
        /// Other sessions cannot take update/exclusive locks on those rows until released.
        /// </summary>
        UpdLock = 2,

        /// <summary>
        /// NOWAIT.
        /// If a needed lock cannot be acquired, fails immediately with a lock timeout error
        /// instead of waiting for the lock to be released.
        /// </summary>
        NoWait = 4
    }
}
