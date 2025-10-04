using SharpOrm.Builder;

namespace SharpOrm
{
    internal class QueryColumn : Column
    {
        public QueryColumn(string name) : base(name, null)
        {

        }

        protected override string GetName(IReadonlyQueryInfo info)
        {
            if (info is QueryInfo qInfo && qInfo.Joins.Count > 0)
                return info.Config.ApplyNomenclature(string.Format("{0}.{1}", info.TableName.TryGetAlias(), Name));

            return info.Config.ApplyNomenclature(Name);
        }
    }
}
