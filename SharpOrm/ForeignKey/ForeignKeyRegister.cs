using SharpOrm.Builder;
using SharpOrm.ForeignKey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.DataTranslation
{
    internal class ForeignKeyRegister : ForeignKeyNodeBase, INodeCreationListener
    {
        private readonly List<MemberInfo> _fkCollections = new List<MemberInfo>();
        private readonly INodeCreationListener _listener;

        public ForeignKeyRegister(TableInfo rootTableInfo, INodeCreationListener listener) : base(rootTableInfo)
        {
            _listener = listener;
        }

        public ForeignKeyNode RegisterTreePath(IEnumerable<MemberInfo> memberPath)
        {
            var pathList = memberPath.ToList();
            if (pathList.Count == 0) return null;

            var rootNode = GetOrAddChild(pathList[0]);
            var currentNode = rootNode;

            for (int i = 1; i < pathList.Count; i++)
                currentNode = currentNode.GetOrAddChild(pathList[i]);

            return currentNode;
        }

        public void CopyTo(ForeignKeyRegister register)
        {
            register._nodes.AddRange(_nodes);
            register._fkCollections.AddRange(_fkCollections);
        }

        public bool IsFkCollection(MemberInfo member)
        {
            return _fkCollections.Contains(member);
        }

        public Column[] GetAllColumn()
        {
            var names = new List<Column>();

            foreach (var item in TableInfo.Columns)
                if (item.ForeignInfo == null)
                    names.Add(new Column($"{TableInfo.Name}.{item.Name}", ""));

            foreach (var node in GetAllNodes())
                if (!node.IsCollection)
                    foreach (var column in node.Columns)
                        if (column.ForeignInfo == null)
                            names.Add(column.Column);

            return names.ToArray();
        }

        public bool HasAnyNonCollection()
        {
            foreach (var node in GetAllNodes())
                if (!node.IsCollection)
                    return true;

            return false;
        }

        public IEnumerable<ForeignKeyNode> GetAllNodes()
        {
            foreach (var child in _nodes)
                foreach (var descendant in child.GetAllNodes())
                    yield return descendant;
        }

        public override string GetTreePrefix()
        {
            return string.Empty;
        }

        void INodeCreationListener.Created(ForeignKeyNode node)
        {
            if (node.IsCollection)
                _fkCollections.Add(node.Member);

            _listener.Created(node);
        }

        public override string ToString()
        {
            return $"{TableInfo.Name} (Root)";
        }

        protected override ForeignKeyNode CreateNode(ColumnInfo memberColumnInfo, TableInfo memberTableInfo, bool isCollection)
        {
            var node = new ForeignKeyNode(this, memberTableInfo, memberColumnInfo, TableInfo, GetTreePrefix(), isCollection);
            ((INodeCreationListener)this).Created(node);
            return node;
        }
    }
}
