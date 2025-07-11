﻿using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder
{
    public class BatchQueryBuilder : QueryBuilder
    {
        private readonly List<SqlExpression> expressions = new List<SqlExpression>();
        private readonly BuilderCursor checkpointCursor = new BuilderCursor();
        private readonly BuilderCursor cacheCursor = new BuilderCursor();
        private bool useCacheCursor = false;

        internal BatchQueryBuilder(IReadonlyQueryInfo info) : base(info)
        {

        }

        public BatchQueryBuilder Remove(int index, int count)
        {
            query.Remove(index, count);
            return this;
        }

        public BatchQueryBuilder RestoreCursor()
        {
            useCacheCursor = false;

            cacheCursor.Params = 0;
            cacheCursor.Text = 0;

            return this;
        }

        public BatchQueryBuilder SetCursor(int textIndex, int paramsIndex)
        {
            useCacheCursor = true;
            cacheCursor.Text = textIndex;
            cacheCursor.Params = paramsIndex;

            return this;
        }

        public BatchQueryBuilder CreateSavePoint()
        {
            checkpointCursor.Params = parameters.Count;
            checkpointCursor.Text = query.Length;
            return this;
        }

        public BatchQueryBuilder BuildSavePoint()
        {
            string checkpointText = GetCheckpointText();
            if (!string.IsNullOrEmpty(checkpointText))
                expressions.Add(new SqlExpression(false, checkpointText, GetCheckpointParams()));

            ClearCheckpointParams();
            ClearCheckpointText();

            checkpointCursor.Params = 0;
            checkpointCursor.Text = 0;
            return this;
        }

        public BatchQueryBuilder ResetSavePoint()
        {
            checkpointCursor.Params = 0;
            checkpointCursor.Text = 0;
            return this;
        }

        public override QueryBuilder Add(string raw)
        {
            if (!useCacheCursor)
                return base.Add(raw);

            query.Insert(cacheCursor.Text, raw);
            cacheCursor.Text += raw.Length;

            return this;
        }

        protected override QueryBuilder InternalAddParam(object value)
        {
            if (!useCacheCursor)
                return base.InternalAddParam(value);

            parameters.Insert(cacheCursor.Params, value);
            cacheCursor.Params++;
            return Add("?");
        }

        private string GetCheckpointText()
        {
            if (checkpointCursor.Text == 0)
                return query.ToString();

            return query.ToString(0, checkpointCursor.Text);
        }

        private object[] GetCheckpointParams()
        {
            if (checkpointCursor.Params == 0)
                return parameters.ToArray();

            return parameters.Take(checkpointCursor.Params).ToArray();
        }

        private void ClearCheckpointParams()
        {
            if (parameters.Count >= checkpointCursor.Params) parameters.RemoveRange(0, checkpointCursor.Params);

            checkpointCursor.Params = -1;
        }

        private void ClearCheckpointText()
        {
            if (query.Length >= checkpointCursor.Text) query.Remove(0, checkpointCursor.Text);

            checkpointCursor.Text = 0;
        }

        public override SqlExpression ToExpression(bool withDeferrer = false, bool throwOnDeferrerFail = true)
        {
            BuildSavePoint();
            if (expressions.Count == 1)
                return expressions[0];

            return new BatchSqlExpression(expressions.ToArray());
        }

        private class BuilderCursor
        {
            public int Text { get; set; }
            public int Params { get; set; }
        }
    }
}
