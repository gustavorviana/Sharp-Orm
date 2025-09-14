using SharpOrm.Builder;
using SharpOrm.Builder.Tables;
using SharpOrm.ForeignKey;
using System;
using System.Collections.Generic;
using System.Data;
using System.Xml.Linq;

namespace SharpOrm.DataTranslation.Reader
{
    internal class ObjectRecordReader : BaseRecordReader
    {
        private readonly ReaderObject _mainObject;
        private readonly ForeignInfo _foreignInfo;

        public ObjectRecordReader(ForeignInfo foreign, TableInfo table, IDataReader reader) : base(reader, table._registry)
        {
            _foreignInfo = foreign;
            _mainObject = new ReaderObject(this, _foreignInfo?.Node, table.Type, table.Columns);
        }

        protected override object OnRead() => _mainObject.Read();

        #region Auxiliar classes

        private class ReaderObject
        {
            private readonly ObjectRecordReader _recordReader;
            private readonly List<ReaderObject> _nodes = new List<ReaderObject>();
            private readonly List<ReaderColumn> _columns = new List<ReaderColumn>();
            private readonly ObjectActivator _activator;
            private readonly ColumnInfo _parentColumn;
            private readonly ForeignKeyNodeBase _fkNode;
            private readonly string _prefix;

            private IDataReader Reader => _recordReader.Reader;

            private ReaderObject(ObjectRecordReader recordReader, ForeignKeyNodeBase fkNode, IColumnNode node)
                : this(recordReader, fkNode, node.Column.Type, node)
            {
                _parentColumn = node.Column;
            }

            public ReaderObject(ObjectRecordReader recordReader, ForeignKeyNodeBase fkNode, Type type, IWithColumnNode nodes, string prefix = null)
            {
                _prefix = prefix;
                _fkNode = fkNode;
                _recordReader = recordReader;
                _activator = new ObjectActivator(type, Reader, _recordReader.Registry);

                foreach (var node in nodes.Nodes)
                    MapNode(node);
            }

            private void MapNode(IColumnNode node)
            {
                if (node.Nodes.Count > 0)
                {
                    foreach (var childNode in node.Nodes)
                        _nodes.Add(new ReaderObject(_recordReader, _fkNode?.Get(node.Column._column), node));

                    return;
                }

                if (node.Column.ForeignInfo != null)
                {
                    MapForeign(node);
                    return;
                }

                if (node.Column.Translation == null)
                    return;

                var index = IndexOf(node.Column.Name);
                if (index >= 0)
                {
                    _columns.Add(new ReaderColumn(node.Column, index));
                }
            }

            private void MapForeign(IColumnNode node)
            {
                if (_fkNode == null)
                    return;

                int index = IndexOf(node.Column.ForeignInfo.ForeignKey);
                var colFkNode = _fkNode.Get(node.Column._column);
                if (index < 0)
                    return;

                if (colFkNode == null)
                {
                    if (_recordReader._foreignInfo.LoadForeign)
                        _columns.Add(new UnusedFkReaderColumn(_recordReader.Registry, node.Column, index));
                    return;
                }

                if (node.IsCollection)
                {
                    _columns.Add(new FkCollectionReaderColumn(_recordReader, colFkNode, index));
                    return;
                }

                var prefix = _prefix ?? string.Empty;
                var table = _recordReader.Registry.GetTable(node.Column.Type);
                _columns.Add(new FkReaderColumn(_recordReader, colFkNode, node, table, prefix, index));
            }

            private int IndexOf(string name)
            {
                if (string.IsNullOrEmpty(_prefix))
                    return Reader.GetIndexOf(name);

                return Reader.GetIndexOf($"{_prefix}c_{name}");
            }

            public object Read()
            {
                var instance = _activator.CreateInstance(Reader);

                foreach (var column in _columns)
                    column.Set(instance, Reader);

                foreach (var node in _nodes)
                {
                    var pi = node.Read();
                    (node._parentColumn as ColumnTreeInfo).InternaRawSet(instance, pi);
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
                (Column as ColumnTreeInfo).InternalSet(owner, GetReaderValue(reader));
            }

            protected object GetReaderValue(IDataReader reader)
            {
                return reader[_index];
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

        private class FkReaderColumn : ReaderColumn
        {
            private readonly ReaderObject _readerObject;

            public FkReaderColumn(ObjectRecordReader recordReader, ForeignKeyNodeBase fkNode, IColumnNode node, TableInfo table, string prefix, int index) : base(node.Column, index)
            {
                prefix += $"{node.Column.PropName}_";
                _readerObject = new ReaderObject(recordReader, fkNode, node.Column.Type, table.Columns, prefix);
            }

            public override void Set(object owner, IDataReader reader)
            {
                (Column as ColumnTreeInfo).InternaRawSet(owner, _readerObject.Read());
            }
        }

        private class FkCollectionReaderColumn : ReaderColumn
        {
            private readonly ObjectRecordReader _recordReader;
            private readonly ForeignKeyNode _node;

            public FkCollectionReaderColumn(ObjectRecordReader recordReader, ForeignKeyNode node, int index) : base(node.ColumnInfo, index)
            {
                _recordReader = recordReader;
                _node = node;
            }

            public override void Set(object owner, IDataReader reader)
            {
                var value = GetReaderValue(reader);

                if (_recordReader._foreignInfo?.Loader == null)
                    Column.SetRaw(owner, ObjIdFkQueue.MakeObjWithId(_recordReader.Registry, Column, value));
                else
                    _recordReader._foreignInfo?.Loader.EnqueueForeign(owner, _recordReader.Registry, value, _node);
            }
        }

        private class UnusedFkReaderColumn : ReaderColumn
        {
            private readonly TranslationRegistry _registry;

            public UnusedFkReaderColumn(TranslationRegistry registry, ColumnInfo column, int index) : base(column, index)
            {
                _registry = registry;
            }

            public override void Set(object owner, IDataReader reader)
            {
                (Column as ColumnTreeInfo).InternaRawSet(owner, ObjIdFkQueue.MakeObjWithId(_registry, Column, GetReaderValue(reader)));
            }
        }

        #endregion
    }
}
