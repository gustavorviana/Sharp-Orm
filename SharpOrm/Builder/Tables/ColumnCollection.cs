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
    public class ColumnCollection : IReadOnlyCollection<ColumnInfo>, IWithColumnNode
    {
        private readonly Dictionary<string, ColumnInfo[]> _columnLookup;
        public IReadOnlyList<IColumnNode> Nodes { get; }

        public int Count => _columnLookup.Count;

        internal ColumnCollection(ColumnNode[] nodes, Dictionary<string, ColumnInfo[]> columnLookup)
        {
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _columnLookup = columnLookup ?? throw new ArgumentNullException(nameof(columnLookup));
        }

        public IEnumerator<ColumnInfo> GetEnumerator() => _columnLookup.Values.Select(x => x.FirstOrDefault()).GetEnumerator();

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

        private IColumnNode FindInTree(string[] path)
        {
            if (path == null || path.Length == 0)
                return null;

            var currentNode = Nodes.FirstOrDefault(x => x.Column.PropName == path[0]);

            for (int i = 1; i < path.Length && currentNode != null; i++)
                currentNode = currentNode.Nodes.FirstOrDefault(x => x.Column.PropName == path[i]);

            return currentNode;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal class ColumnNode : IColumnNode
        {
            public ColumnInfo Column { get; }
            public ColumnNode[] Nodes { get; }
            public bool IsCollection { get; }

            IReadOnlyList<IColumnNode> IWithColumnNode.Nodes => Nodes;

            internal ColumnNode(ColumnInfo column, ColumnNode[] nodes)
            {
                Column = column ?? throw new ArgumentNullException(nameof(column));
                Nodes = nodes ?? DotnetUtils.EmptyArray<ColumnNode>();
                IsCollection = ReflectionUtils.IsCollection(column.Type);
            }

            internal IEnumerable<ColumnInfo> Flatten()
            {
                if (Nodes.Length == 0)
                {
                    yield return Column;
                    yield break;
                }

                foreach (var child in Nodes)
                    foreach (var descendant in child.Flatten())
                        yield return descendant;
            }
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