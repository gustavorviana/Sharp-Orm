using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using SharpOrm.Interceptors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm
{
    /// <summary>
    /// Class responsible for executing operations based on interceptor context
    /// </summary>
    internal class InterceptorOperationExecutor
    {
        private ObjectReader _objReader;
        private readonly TableInfo _tableInfo;
        private readonly Query _query;

        private bool HasInterceptor => _query.Config.SaveInterceptor != null;

        private bool ValidateModelOnSave { get; set; }
        public bool IgnoreTimestamps { get; set; }

        public InterceptorOperationExecutor(Query query, TableInfo table)
        {
            _tableInfo = table ?? throw new ArgumentNullException(nameof(table));
            _query = query ?? throw new ArgumentNullException(nameof(query));

            _objReader = new ObjectReader(table);
        }

        public int Update<T>(T item)
        {
            ConfigureReader(ReadMode.None, false);

            if (!HasInterceptor)
                return _query.Update(_objReader.ReadCells(item));

            return ExecuteEntry(TriggerContext<T>(EntryState.Update, item));
        }

        public int Create<T>(T item)
        {
            ConfigureReader(ReadMode.ValidOnly, true);

            if (!HasInterceptor)
                return _query.Insert(_objReader.ReadCells(item));

            return ExecuteEntry(TriggerContext<T>(EntryState.Add, item));
        }

        private int ExecuteEntry(DbEntry entry)
        {

            return 0;
        }

        private DbEntry TriggerContext<T>(EntryState state, T item)
        {
            var context = CreateContext(state, item);
            _query.Config.SaveInterceptor.OnIntercept(context);
            return context.Entries().First();
        }

        private ModelInterceptorContext CreateContext<T>(EntryState state, T item)
        {
            return new ModelInterceptorContext(_query).AddObject(_objReader, _tableInfo, item, state);
        }

        private ObjectReader ConfigureReader(ReadMode pkReadMode, bool isCreate)
        {
            _objReader.ReadFk = _query.Config.LoadForeign;
            _objReader.Validate = ValidateModelOnSave;

            _objReader.IgnoreTimestamps = IgnoreTimestamps;
            _objReader.PrimaryKeyMode = pkReadMode;
            _objReader.IsCreate = isCreate;

            return _objReader;
        }
    }
}
