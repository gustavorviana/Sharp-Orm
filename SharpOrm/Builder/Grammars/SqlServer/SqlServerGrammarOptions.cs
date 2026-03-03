using System;
using System.Collections.Generic;

namespace SharpOrm.Builder.Grammars.SqlServer
{
    /// <summary>
    /// Represents the grammar options for SQL Server.
    /// </summary>
    public class SqlServerGrammarOptions : IGrammarOptions
    {
        /// <summary>
        /// Defines how SQL Server reads data from this table reference.
        /// 
        /// Affects ONLY read operations (SELECT).
        /// Controls read isolation behavior such as allowing dirty reads,
        /// forcing lock-based reads or serializable semantics.
        /// </summary>
        public SqlServerReadIsolationHint ReadIsolation { get; set; }

        /// <summary>
        /// Defines the lock granularity SQL Server should prefer
        /// when accessing this table reference.
        /// 
        /// Applies to SELECT, UPDATE and DELETE operations.
        /// Controls whether locks are taken at row, page or table level.
        /// </summary>
        public SqlServerLockHint LockHint { get; set; }

        /// <summary>
        /// Defines concurrency-related behaviors when encountering locked rows.
        /// 
        /// Applies to SELECT, UPDATE and DELETE operations.
        /// These options can be combined.
        /// </summary>
        public SqlServerConcurrencyHint Concurrency { get; set; }

        /// <summary>
        /// Defines how SQL Server should choose the execution plan
        /// for this table reference.
        /// 
        /// Applies to SELECT, UPDATE and DELETE operations.
        /// Forces seek or scan behavior on indexes.
        /// </summary>
        public SqlServerPlanHint PlanHint { get; set; }

        /// <summary>
        /// INDEX(name | id).
        /// Forces SQL Server to use a specific index.
        /// Cannot be combined with ForceScan.
        /// </summary>
        public string Index { get; set; }

        /// <summary>
        /// This property will be removed in version 4.x.
        /// 
        /// Use <see cref="ReadIsolation"/> with
        /// <see cref="SqlServerReadIsolationHint.NoLock"/> or
        /// <see cref="SqlServerReadIsolationHint.ReadUncommitted"/> instead.
        /// 
        /// When set to true, this property forces the NOLOCK table hint,
        /// regardless of the value defined in <see cref="ReadIsolation"/>.
        /// </summary>
        [Obsolete(
            "NoLock is deprecated and will be removed in version 4.x. " +
            "Use ReadIsolation = SqlServerReadIsolationHint.NoLock or ReadUncommitted instead."
        )]
        public bool NoLock
        {
            get => ReadIsolation == SqlServerReadIsolationHint.NoLock ||
                   ReadIsolation == SqlServerReadIsolationHint.ReadUncommitted;

            set => ReadIsolation = value
                ? SqlServerReadIsolationHint.NoLock
                : SqlServerReadIsolationHint.None;
        }

        internal void WriteTo(QueryBuilder builder, bool isSelect)
        {
            var hints = GetHints(isSelect);

            if (hints.Count > 0)
                builder.Add(" WITH (" + string.Join(", ", hints) + ")");
        }

        internal List<string> GetHints(bool isSelect)
        {
            var hints = new List<string>();
            if (isSelect)
                WriteReadIsolationHints(hints);

            WriteLockHints(hints);
            WriteConcurrencyHints(hints);
            WritePlanHints(hints);
            WriteIndexHint(hints);

            return hints;
        }

        /// <summary>
        /// Writes READ ISOLATION hints (NOLOCK, READUNCOMMITTED, etc).
        /// These hints are valid ONLY for read operations (SELECT).
        /// </summary>
        private void WriteReadIsolationHints(List<string> hints)
        {
            if (ReadIsolation != SqlServerReadIsolationHint.None)
                hints.Add(ReadIsolation.ToString().ToUpperInvariant());
        }

        /// <summary>
        /// Writes LOCK GRANULARITY hints (ROWLOCK, PAGLOCK, TABLOCK, TABLOCKX).
        /// These hints are valid for SELECT, UPDATE and DELETE.
        /// </summary>
        private void WriteLockHints(List<string> hints)
        {
            if (LockHint != SqlServerLockHint.None)
                hints.Add(LockHint.ToString().ToUpperInvariant());
        }

        /// <summary>
        /// Writes CONCURRENCY hints (READPAST, UPDLOCK, NOWAIT).
        /// These hints are valid for SELECT, UPDATE and DELETE.
        /// </summary>
        private void WriteConcurrencyHints(List<string> hints)
        {
            if (Concurrency.HasFlag(SqlServerConcurrencyHint.ReadPast))
                hints.Add("READPAST");

            if (Concurrency.HasFlag(SqlServerConcurrencyHint.UpdLock))
                hints.Add("UPDLOCK");

            if (Concurrency.HasFlag(SqlServerConcurrencyHint.NoWait))
                hints.Add("NOWAIT");
        }

        /// <summary>
        /// Writes PLAN hints (FORCESEEK, FORCESCAN).
        /// These hints are valid for SELECT, UPDATE and DELETE.
        /// </summary>
        private void WritePlanHints(List<string> hints)
        {
            if (PlanHint != SqlServerPlanHint.None)
                hints.Add(PlanHint.ToString().ToUpperInvariant());
        }

        /// <summary>
        /// Writes INDEX hint.
        /// Valid for SELECT, UPDATE and DELETE (never for INSERT target).
        /// </summary>
        private void WriteIndexHint(List<string> hints)
        {
            if (string.IsNullOrWhiteSpace(Index))
                return;

            if (PlanHint == SqlServerPlanHint.ForceScan)
                throw new InvalidOperationException(
                    "INDEX cannot be used together with FORCESCAN."
                );

            hints.Add($"INDEX({Index})");
        }

        internal bool HasHints()
        {
            return ReadIsolation != SqlServerReadIsolationHint.None ||
                LockHint != SqlServerLockHint.None ||
                Concurrency != SqlServerConcurrencyHint.None ||
                PlanHint != SqlServerPlanHint.None ||
                !string.IsNullOrWhiteSpace(Index);
        }
    }
}
