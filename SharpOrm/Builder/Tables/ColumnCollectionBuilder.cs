using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using SharpOrm.ForeignKey;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder.Tables
{
    internal class ColumnCollectionBuilder : ITreeAdd<ColumnCollectionBuilder.BuilderNode>, ITreeNodes<ColumnCollectionBuilder.BuilderNode>
    {
        private readonly Dictionary<string, List<ColumnInfo>> _columnLookup = new Dictionary<string, List<ColumnInfo>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<BuilderNode> _nodes = new List<BuilderNode>();

        List<BuilderNode> ITreeNodes<BuilderNode>.Nodes => _nodes;

        public ITreeAdd<BuilderNode> Add(ColumnInfo column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            var node = new BuilderNode(this, column);
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

        public ColumnCollection Build()
        {
            var builtNodes = _nodes.Select(x => x.Build()).Where(x => x != null).ToArray();

            var finalLookup = _columnLookup.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);

            return new ColumnCollection(builtNodes, finalLookup);
        }

        public bool Remove(SqlMember member)
        {
            ITreeNodes<BuilderNode> node = this;
            foreach (var item in member.Path)
                node = node?.Nodes?.FirstOrDefault(x => x.Column._column.Name == item.Member.Name);

            if (node == null)
                return false;

            var index = node.Nodes.FindIndex(x => x.Column._column.Name == member.Member.Name);
            if (index < 0)
                return false;

            var toRemoveNode = node.Nodes[index];
            RemoveLookup(toRemoveNode.Column);
            node.Nodes.RemoveAt(index);

            return true;
        }

        public class BuilderNode : ITreeAdd<BuilderNode>, ITreeNodes<BuilderNode>
        {
            private readonly ColumnCollectionBuilder _owner;
            private readonly List<BuilderNode> _nodes = new List<BuilderNode>();
            private bool _isLeaf = true;

            public ColumnInfo Column { get; }
            public bool IsCollection { get; }

            List<BuilderNode> ITreeNodes<BuilderNode>.Nodes => _nodes;

            public BuilderNode(ColumnCollectionBuilder owner, ColumnInfo column)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                Column = column ?? throw new ArgumentNullException(nameof(column));
                IsCollection = ReflectionUtils.IsCollection(column.Type);
            }

            public ITreeAdd<BuilderNode> Add(ColumnInfo column)
            {
                var node = new BuilderNode(_owner, column);

                if (_nodes.Count == 0)
                    _owner.RemoveLookup(Column);

                _isLeaf = false;
                _nodes.Add(node);
                _owner.AddLookup(column);

                return node;
            }

            internal ColumnCollection.ColumnNode Build()
            {
                if (!_isLeaf && _nodes.Count == 0)
                    return null;

                var childNodes = _nodes.Select(x => x.Build()).Where(x => x != null).ToArray();
                return new ColumnCollection.ColumnNode(Column, childNodes);
            }

            public override string ToString()
            {
                return $"BuilderNode: Member({Column._column.Name}); Column({Column.Name}); Nodes({_nodes.Count})";
            }
        }
    }
}
