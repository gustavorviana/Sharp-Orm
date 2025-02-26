using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace SharpOrm.Builder
{
    internal class MemberTreeNode
    {
        private ColumnMapInfo columnInfo;

        public List<MemberTreeNode> Children { get; } = new List<MemberTreeNode>();
        public MemberInfo Member { get; }
        internal string prefix;

        public MemberTreeNode(MemberInfo member)
        {
            this.Member = member;
        }

        internal MemberTreeNode GetOrAdd(MemberInfo member)
        {
            var children = Children.FirstOrDefault(x => x.Member == member);
            if (children != null)
                return children;

            children = new MemberTreeNode(member);
            Children.Add(children);
            return children;
        }

        internal MemberTreeNode MapChildren(MemberInfo member, bool nested)
        {
            var type = ReflectionUtils.GetMemberType(member);

            foreach (var property in type.GetProperties(Bindings.PublicInstance))
                if (ColumnInfo.CanWork(property))
                    if (IsNative(property.PropertyType)) Children.Add(new MemberTreeNode(property));
                    else Children.Add(new MemberTreeNode(property).MapChildren(property, nested));

            foreach (var field in type.GetFields(Bindings.PublicInstance))
                if (ColumnInfo.CanWork(field))
                    if (IsNative(field.FieldType)) Children.Add(new MemberTreeNode(field));
                    else Children.Add(new MemberTreeNode(field).MapChildren(field, nested));

            return this;
        }

        internal static bool IsNative(Type type)
        {
            return TranslationUtils.IsNative(type, false) || ReflectionUtils.IsCollection(type);
        }

        internal ColumnMapInfo GetColumn()
        {
            if (this.columnInfo != null) return this.columnInfo;
            return this.columnInfo = new ColumnMapInfo(this.Member);
        }

        internal IEnumerable<ColumnTreeInfo> BuildTree(List<MemberInfo> root, TranslationRegistry registry, string prefix, bool isRootPrefix = false)
        {
            if (this.Children.Count == 0)
            {
                yield return this.Build(root, registry, GetValidName(root, prefix, this, isRootPrefix));
                yield break;
            }

            foreach (var child in this.Children)
            {
                root.Add(child.Member);

                foreach (var result in child.BuildTree(root, registry, child.Children.Count == 0 ? this.prefix ?? prefix : GetValidName(root, prefix, child), !string.IsNullOrEmpty(this.prefix)))
                    yield return result;

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
                return this.prefix;

            return $"{prefix}_";
        }

        private ColumnTreeInfo Build(List<MemberInfo> path, TranslationRegistry registry, string fullName)
        {
            this.GetColumn().builded = true;

            if (string.IsNullOrEmpty(columnInfo._name))
                columnInfo._name = fullName;

            return new ColumnTreeInfo(path, columnInfo, registry);
        }
    }
}
