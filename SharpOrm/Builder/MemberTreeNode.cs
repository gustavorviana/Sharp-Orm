using SharpOrm.Builder.Tables;
using SharpOrm.DataTranslation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder
{
    internal class MemberTreeNode
    {
        private ColumnMapInfo _columnInfo;

        public List<MemberTreeNode> Children { get; } = new List<MemberTreeNode>();
        public MemberInfo Member { get; }
        internal string _prefix;

        public MemberTreeNode(MemberInfo member, MapNestedAttribute nestedInfo)
        {
            Member = member;
            _prefix = nestedInfo?.Prefix;
        }

        internal MemberTreeNode GetOrAdd(MemberInfo member)
        {
            var children = Children.FirstOrDefault(x => x.Member == member);
            if (children != null)
                return children;

            children = new MemberTreeNode(member, null);
            Children.Add(children);
            return children;
        }

        internal MemberTreeNode MapChildren(MemberInfo parentMember, NestedMode mode)
        {
            var type = ReflectionUtils.GetMemberType(parentMember);

            if (IsValueMember(parentMember))
                return this;

            foreach (var member in type.GetProperties(Bindings.PublicInstance))
                if (!Children.Any(x => x.Member == member) && MapNode(member, mode) is MemberTreeNode node)
                    Children.Add(node);

            foreach (var member in type.GetFields(Bindings.PublicInstance))
                if (!Children.Any(x => x.Member == member) && MapNode(member, mode) is MemberTreeNode node)
                    Children.Add(node);

            return this;
        }

        internal static MemberTreeNode MapNode(MemberInfo memberInfo, NestedMode mode)
        {
            if (!ColumnInfo.CanWork(memberInfo))
                return null;

            var attr = GetNestedInfo(memberInfo);
            if (attr == null && !IsValueMember(memberInfo) && !NeedMapNested(memberInfo, mode))
                return null;

            return new MemberTreeNode(memberInfo, attr).MapChildren(memberInfo, mode);
        }

        internal static bool NeedMapNested(MemberInfo member, NestedMode mode)
        {
            return ReflectionUtils.GetMemberType(member).GetCustomAttribute<OwnedAttribute>() != null || mode == NestedMode.All;
        }

        internal static MapNestedAttribute GetNestedInfo(MemberInfo member)
        {
            return member.GetCustomAttribute<MapNestedAttribute>();
        }

        internal static bool IsValueMember(MemberInfo member)
        {
            return TranslationUtils.IsNative(ReflectionUtils.GetMemberType(member), false) ||
                ColumnInfo.IsForeign(member) ||
                member.GetCustomAttribute<SqlConverterAttribute>() != null ||
                member.GetCustomAttribute<ColumnAttribute>() != null ||
                member.DeclaringType.GetCustomAttribute<SqlConverterAttribute>() != null;
        }

        internal ColumnMapInfo GetColumn(TranslationRegistry registry)
        {
            if (_columnInfo != null) return _columnInfo;
            return _columnInfo = new ColumnMapInfo(Member, registry);
        }

        internal void BuildTree(List<MemberInfo> root, ITreeAdd<ColumnCollectionBuilder.BuilderNode> owner, TranslationRegistry registry, string prefix, bool isRootPrefix = false)
        {
            owner = owner.Add(Build(root, registry, GetValidName(root, prefix, this, isRootPrefix)));

            foreach (var child in Children)
            {
                root.Add(child.Member);
                child.BuildTree(root, owner, registry, child.Children.Count == 0 ? _prefix ?? prefix : GetValidName(root, prefix, child), !string.IsNullOrEmpty(_prefix));
                root.RemoveAt(root.Count - 1);
            }
        }

        private string GetValidName(List<MemberInfo> root, string prefix, MemberTreeNode child, bool isRootPrefix = false)
        {
            if (isRootPrefix)
                return prefix + child.Member.Name;

            prefix = GetValidPrefix(prefix);
            return string.IsNullOrEmpty(prefix) ? GetPathName(root) : prefix + child.Member.Name;
        }

        private string GetPathName(List<MemberInfo> root)
        {
            return ReflectionUtils.ToPath(root, '_');
        }

        private string GetValidPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return _prefix;

            return $"{prefix}_";
        }

        private ColumnTreeInfo Build(List<MemberInfo> path, TranslationRegistry registry, string fullName)
        {
            GetColumn(registry).builded = true;

            if (string.IsNullOrEmpty(_columnInfo._name))
                _columnInfo._name = fullName;

            return new ColumnTreeInfo(path, _columnInfo, registry);
        }
    }
}
