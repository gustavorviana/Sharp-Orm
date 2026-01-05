namespace SharpOrm.Builder.Grammars.SqlServer
{
    /// <summary>
    /// SQL Server table hints that influence plan choice for a specific table reference.
    /// Only one option should be used at a time.
    /// </summary>
    public enum SqlServerPlanHint
    {
        /// <summary>
        /// No plan hint is emitted.
        /// SQL Server chooses the execution plan.
        /// </summary>
        None,

        /// <summary>
        /// FORCESEEK.
        /// Forces an index seek operation for the table reference.
        /// Can fail if a seek is not possible with available indexes.
        /// </summary>
        ForceSeek,

        /// <summary>
        /// FORCESCAN.
        /// Forces a scan operation (index scan or table scan) for the table reference.
        /// Can be slower for selective predicates but useful in some edge cases.
        /// </summary>
        ForceScan
    }
}
