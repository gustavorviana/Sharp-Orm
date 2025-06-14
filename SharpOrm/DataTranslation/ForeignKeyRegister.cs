using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpOrm.DataTranslation
{
    internal class ForeignKeyRegister : ForeignKeyNodeBase
    {
        private readonly List<MemberInfo> _fkCollections = new List<MemberInfo>();

        public ForeignKeyRegister(TableInfo rootTableInfo) : base(rootTableInfo)
        {
        }

        public IEnumerable<ForeignKeyTreeNode> RegisterTreePath(IEnumerable<MemberInfo> memberPath)
        {
            var pathList = memberPath.ToList();
            if (pathList.Count == 0) yield break;

            var createdNodes = new List<ForeignKeyTreeNode>();
            var rootMember = pathList[0];
            if (GetOrAddChild(rootMember, out var rootNode))
                yield return rootNode;

            var currentNode = rootNode;

            for (int i = 1; i < pathList.Count; i++)
            {
                var isNew = currentNode.GetOrAddChild(pathList[i], out currentNode);

                if (isNew)
                {
                    if (currentNode.IsCollection)
                        _fkCollections.Add(currentNode.Member);

                    createdNodes.Add(currentNode);
                }
            }

            foreach (var node in createdNodes)
                yield return node;
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

        public override bool GetOrAddChild(MemberInfo member, out ForeignKeyTreeNode node)
        {
            if (!base.GetOrAddChild(member, out node))
                return false;

            if (node.IsCollection)
                _fkCollections.Add(member);

            return true;
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

        public IEnumerable<ForeignKeyTreeNode> GetAllNodes()
        {
            foreach (var child in _nodes)
                foreach (var descendant in child.GetAllNodes())
                    yield return descendant;
        }

        public override string GetTreePrefix()
        {
            return string.Empty;
        }
    }
}
