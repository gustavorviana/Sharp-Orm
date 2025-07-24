using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Table
{
    public class ColumnType : IColumnTypeMap
    {
        private readonly string _name;
        private readonly Type _type;

        public ColumnType(Type type, string name)
        {
            _type = type;
            _name = name;
        }

        public string Build(DataColumn column) => _name;
        public bool CanWork(Type type) => _type == type;
    }
}
