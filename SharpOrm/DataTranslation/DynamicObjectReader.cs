using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.DataTranslation
{
    public class DynamicObjectReader : ObjectReaderBase
    {
        private string[] _columns = DotnetUtils.EmptyArray<string>();

        private bool _needContains;

        public DynamicObjectReader(TableInfo table) : base(table)
        {
        }

        public override string[] GetColumnNames()
        {
            return DotnetUtils.EmptyArray<string>();
        }

        public override IEnumerable<Cell> ReadCells(object owner)
        {
            if (owner is IDictionary<string, object> dict)
                return ReadDictCells(dict);

            throw new NotSupportedException("Only IDictionary<string, object> is supported.");
        }

        protected override void SetColumns(string[] columns, bool needContains)
        {
            _columns = columns ?? DotnetUtils.EmptyArray<string>();
            _needContains = needContains;
        }

        protected override void SetExpression<K>(Expression<ColumnExpression<K>> expression, bool needContains)
        {
            _columns = GetMembers(expression).Select(x => x.Name).ToArray();
            _needContains = needContains;
        }

        private IEnumerable<Cell> ReadDictCells(IDictionary<string, object> owner)
        {
            return owner
                .Where(item => IsAllowedName(item.Key) || (IsKey(item.Key) && CanUseKeyValue(item.Value)))
                .Select(item => new Cell(item.Key, item.Value));
        }

        protected bool IsAllowedName(string name)
        {
            return _columns.Length == 0 ||
            _columns.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)) == _needContains;
        }

        private static bool IsKey(string name)
        {
            return name.Equals("id", StringComparison.OrdinalIgnoreCase);
        }
    }
}
