using SharpOrm.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.Builder.Tables
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(WeakRef_DebugView<>))]
    public class ColumnCollection : IReadOnlyCollection<ColumnInfo>, ITreeAdd<ColumnCollection.ColumnNode>, IWithColumnNode
    {
        private readonly Dictionary<string, List<ColumnInfo>> _columnLookup = new Dictionary<string, List<ColumnInfo>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ColumnNode> _nodes = new List<ColumnNode>();

        public IReadOnlyList<IColumnNode> Nodes => _nodes;

        public int Count => _columnLookup.Count;

        public IEnumerator<ColumnInfo> GetEnumerator() => _columnLookup.Values.Select(x => x.FirstOrDefault()).GetEnumerator();

        internal ColumnCollection()
        {

        }

        public ColumnInfo Find(string name)
        {
            if (_columnLookup.TryGetValue(name, out var columns))
                return columns.FirstOrDefault();

            return null;
        }

        public ColumnInfo Find<T>(Expression<ColumnExpression<T>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            var path = ExpressionUtils<T>
                .GetMemberPath(expression, true)
                .Select(x => x.Name)
                .Reverse()
                .ToArray();

            return FindInTree(path)?.Column;
        }

        private ColumnNode FindInTree(string[] path)
        {
            if (path == null || path.Length == 0)
                return null;

            var currentNode = _nodes.FirstOrDefault(x => x.Column.PropName == path[0]);

            for (int i = 1; i < path.Length && currentNode != null; i++)
                currentNode = currentNode.Nodes.FirstOrDefault(x => x.Column.PropName == path[i]);

            return currentNode;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        ITreeAdd<ColumnCollection.ColumnNode> ITreeAdd<ColumnCollection.ColumnNode>.Add(ColumnInfo column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            var node = new ColumnNode(this, column);
            _nodes.Add(node);
            AddLookup(column);

            return node;
        }

        private void AddLookup(ColumnInfo column)
        {
            if (!_columnLookup.TryGetValue(column.Name, out var list))
            {
                list = new List<ColumnInfo>();
                _columnLookup.Add(column.Name, list);
            }

            list.Add(column);
        }

        private bool RemoveLookup(ColumnInfo column)
        {
            if (_columnLookup.TryGetValue(column.Name, out var list))
            {
                list.Remove(column);
                if (list.Count == 0)
                    _columnLookup.Remove(column.Name);
                return true;
            }
            return false;

        }

        internal ColumnCollection Build()
        {
            foreach (var item in _nodes)
                item.Build();

            return this;
        }

        internal class ColumnNode : ITreeAdd<ColumnCollection.ColumnNode>, IColumnNode
        {
            private readonly ColumnCollection _owner;

            public ColumnInfo Column { get; }
            public List<ColumnNode> Nodes { get; } = new List<ColumnNode>();

            IReadOnlyList<IColumnNode> IWithColumnNode.Nodes => Nodes;

            public bool IsCollection { get; }

            public ColumnNode(ColumnCollection root, ColumnInfo column)
            {
                _owner = root ?? throw new ArgumentNullException(nameof(root));
                Column = column ?? throw new ArgumentNullException(nameof(column));
                IsCollection = ReflectionUtils.IsCollection(column.Type);
            }

            internal IEnumerable<ColumnInfo> Flatten()
            {
                if (Nodes.Count == 0)
                {
                    yield return Column;
                    yield break;
                }

                foreach (var child in Nodes)
                    foreach (var descendant in child.Flatten())
                        yield return descendant;
            }

            internal ColumnNode Add(ColumnInfo column)
            {
                var node = new ColumnNode(_owner, column);

                if (Nodes.Count == 0)
                    _owner.RemoveLookup(Column);

                Nodes.Add(node);
                _owner.AddLookup(column);

                return node;
            }

            internal ColumnNode Build()
            {
                if (Nodes.Count == 0)
                    return this;

                _owner.RemoveLookup(Column);

                foreach (var child in Nodes)
                    child.Build();

                return this;
            }

            ITreeAdd<ColumnCollection.ColumnNode> ITreeAdd<ColumnCollection.ColumnNode>.Add(ColumnInfo column) => Add(column);
        }

        internal sealed class ColumnCollection_DebugView
        {
            private readonly ColumnCollection _collection;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public ColumnInfo[] Items => _collection.ToArray();

            public ColumnCollection_DebugView(ColumnCollection collection)
            {
                _collection = collection;
            }
        }
    }
}