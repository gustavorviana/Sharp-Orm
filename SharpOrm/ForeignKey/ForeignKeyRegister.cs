using SharpOrm.Builder;
using SharpOrm.ForeignKey;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpOrm.DataTranslation
{
    internal class ForeignKeyRegister : ForeignKeyNodeBase, INodeCreationListener
    {
        private readonly List<MemberInfo> _fkCollections = new List<MemberInfo>();
        private readonly INodeCreationListener _listener;
        public override DbName Name { get; }

        public override QueryInfo RootInfo => _listener.Info;

        QueryInfo IWithQueryInfo.Info => _listener.Info;

        public ForeignKeyRegister(TableInfo rootTableInfo, DbName name, INodeCreationListener listener) : base(rootTableInfo)
        {
            _listener = listener;
            Name = name;
        }

        public ForeignKeyNode RegisterTreePath(IEnumerable<MemberInfo> memberPath, bool silent = false)
        {
            var pathList = memberPath.ToList();
            if (pathList.Count == 0) return null;

            var rootNode = GetOrAddChild(pathList[0], silent);
            var currentNode = rootNode;

            for (int i = 1; i < pathList.Count; i++)
                currentNode = currentNode.GetOrAddChild(pathList[i], silent);

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
                    names.Add(new Column($"{Name.TryGetAlias()}.{item.Name}", ""));

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

        protected override ForeignKeyNode CreateNode(ColumnInfo memberColumnInfo, TableInfo memberTableInfo, bool isCollection, bool silent = false)
        {
            var node = new ForeignKeyNode(this, memberTableInfo, memberColumnInfo, this, GetTreePrefix(), isCollection);
            if (!silent)
                ((INodeCreationListener)this).Created(node);
            return node;
        }

        public ForeignKeyNode Get(IList<MemberInfo> path)
        {
            if (path == null || path.Count == 0)
                return null;

            var node = Get(path[0]);
            for (int i = 1; node != null && i < path.Count; i++)
                node = node.Get(path[i]);

            return node;
        }
    }
}
