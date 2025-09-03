using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SharpOrm.Builder.Tables
{
    public class ColumnCollection : IReadOnlyCollection<ColumnInfo>, ITreeAdd<ColumnCollection.ColumnNode>
    {
        private readonly Dictionary<string, List<ColumnInfo>> _columnLookup = new Dictionary<string, List<ColumnInfo>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ColumnNode> _nodes = new List<ColumnNode>();

        public IReadOnlyList<IColumnNode> Nodes => _nodes;

        public int Count => _columnLookup.Count;

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

        private ColumnNode FindInTree(string[] path)
        {
            if (path == null || path.Length == 0)
                return null;

            var currentNode = _nodes.FirstOrDefault(x => x.Column.PropName == path[0]);

            for (int i = 1; i < path.Length && currentNode != null; i++)
                currentNode = currentNode.Children.FirstOrDefault(x => x.Column.PropName == path[i]);

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
            public List<ColumnNode> Children { get; } = new List<ColumnNode>();

            IReadOnlyList<IColumnNode> IColumnNode.Children => Children;

            public ColumnNode(ColumnCollection root, ColumnInfo column)
            {
                _owner = root ?? throw new ArgumentNullException(nameof(root));
                Column = column ?? throw new ArgumentNullException(nameof(column));
                Children = new List<ColumnNode>();
            }

            internal IEnumerable<ColumnInfo> Flatten()
            {
                if (Children.Count == 0)
                {
                    yield return Column;
                    yield break;
                }

                foreach (var child in Children)
                    foreach (var descendant in child.Flatten())
                        yield return descendant;
            }

            internal ColumnNode Add(ColumnInfo column)
            {
                var node = new ColumnNode(_owner, column);

                if (Children.Count == 0)
                    _owner.RemoveLookup(Column);

                Children.Add(node);
                _owner.AddLookup(column);

                return node;
            }

            internal ColumnNode Build()
            {
                if (Children.Count == 0)
                    return this;

                _owner.RemoveLookup(Column);

                foreach (var child in Children)
                    child.Build();

                return this;
            }

            ITreeAdd<ColumnCollection.ColumnNode> ITreeAdd<ColumnCollection.ColumnNode>.Add(ColumnInfo column) => Add(column);
        }
    }
}