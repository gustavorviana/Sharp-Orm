using SharpOrm.Builder;
using SharpOrm.Builder.Tables;
using System;
using System.Collections.Generic;
using System.Data;

namespace SharpOrm.DataTranslation.Reader
{
    internal class ObjectRecordReader : BaseRecordReader
    {
        private readonly ReaderObject _mainObject;

        public ObjectRecordReader(TableInfo table, IDataReader reader, TranslationRegistry registry) : base(reader, registry)
        {
            _mainObject = new ReaderObject(table.Type, table.Columns, reader, registry);
        }

        protected override object OnRead() => _mainObject.Read(Reader);

        #region Auxiliar classes

        private class ReaderObject
        {
            private readonly List<ReaderObject> _nodes = new List<ReaderObject>();
            private readonly List<ReaderColumn> _columns = new List<ReaderColumn>();
            private readonly ObjectActivator _activator;
            private readonly ColumnInfo _parentColumn;

            private ReaderObject(IColumnNode node, IDataReader reader, TranslationRegistry registry)
                : this(node.Column.Type, node, reader, registry)
            {
                _parentColumn = node.Column;
            }

            public ReaderObject(Type type, IWithColumnNode nodes, IDataReader reader, TranslationRegistry registry)
            {
                _activator = new ObjectActivator(type, reader, registry);

                foreach (var node in nodes.Nodes)
                    MapNode(node, reader, registry);
            }

            private void MapNode(IColumnNode node, IDataReader reader, TranslationRegistry registry)
            {
                if (node.Nodes.Count > 0)
                {
                    foreach (var childNode in node.Nodes)
                        _nodes.Add(new ReaderObject(node, reader, registry));

                    return;
                }

                if (node.IsCollection)
                    return;

                int index = reader.GetIndexOf(node.Column.Name);
                if (index < 0)
                    return;

                _columns.Add(new ReaderColumn(node.Column, index));
            }

            public object Read(IDataReader reader)
            {
                var instance = _activator.CreateInstance(reader);

                foreach (var column in _columns)
                    column.Set(instance, reader);

                foreach (var node in _nodes)
                {
                    var pi = node.Read(reader);
                    node._parentColumn.SetRaw(instance, pi);
                }

                return instance;
            }
        }

        private class ReaderColumn
        {
            public ColumnInfo Column { get; }
            private readonly int _index;

            public ReaderColumn(ColumnInfo column, int index)
            {
                Column = column;
                _index = index;
            }

            public virtual void Set(object owner, IDataReader reader)
            {
                (Column as ColumnTreeInfo).InternalSet(owner, reader[_index]);
            }

            public override int GetHashCode()
            {
                return Column.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return Column._column.Equals(obj);
            }

            public override string ToString()
            {
                return Column.ToString();
            }
        }
        #endregion
    }
}
