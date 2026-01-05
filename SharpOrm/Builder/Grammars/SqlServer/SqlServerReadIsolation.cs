namespace SharpOrm.Builder.Grammars.SqlServer
{
    /// <summary>
    /// Controls how SQL Server reads data from a table.
    /// Only one option can be applied at a time.
    /// </summary>
    public enum SqlServerReadIsolationHint
    {
        /// <summary>
        /// No read isolation hint is applied.
        /// Uses the database default isolation level.
        /// </summary>
        None,

        /// <summary>
        /// NOLOCK.
        /// Alias for READUNCOMMITTED.
        /// Reads uncommitted data (dirty reads).
        /// Does not acquire shared locks and ignores exclusive locks.
        /// </summary>
        NoLock,

        /// <summary>
        /// READUNCOMMITTED.
        /// Same behavior as NOLOCK.
        /// Reads uncommitted data (dirty reads).
        /// </summary>
        ReadUncommitted,

        /// <summary>
        /// READCOMMITTEDLOCK.
        /// Forces READ COMMITTED using locks,
        /// even if READ_COMMITTED_SNAPSHOT is enabled.
        /// </summary>
        ReadCommittedLock,

        /// <summary>
        /// HOLDLOCK.
        /// Equivalent to SERIALIZABLE isolation.
        /// Holds locks until the end of the transaction
        /// and prevents phantom reads.
        /// </summary>
        HoldLock
    }
}