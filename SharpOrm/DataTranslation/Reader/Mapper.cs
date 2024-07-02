using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Represents an object that can be mapped from a database reader.
    /// </summary>
    public class Mapper : IMappedObject
    {
        #region Properties\Fields
        private readonly Dictionary<string, InstanceMap> objPath = new Dictionary<string, InstanceMap>();
        private readonly List<MappedColumn> columns = new List<MappedColumn>();
        private readonly TranslationRegistry registry;
        private readonly InstanceMap root;

        /// <summary>
        /// Gets the type of the mapped object.
        /// </summary>
        public Type Type { get; }
        #endregion

        public static Mapper FromMap<T>(TableMap<T> map, TranslationRegistry registry, DbDataReader reader)
        {
            return new Mapper(typeof(T), map.GetPaths(), registry, reader);
        }

        private Mapper(Type type, IEnumerable<ReflectedField> fields, TranslationRegistry registry, DbDataReader reader)
        {
            root = new InstanceMap(type);
            this.registry = registry;

            foreach (var field in fields)
                this.Map(field, reader);
        }

        private void Map(ReflectedField field, DbDataReader reader)
        {
            this.columns.Add(new MappedColumn(this.BuildInstanceTree(field), this.GetColumn(field.Path[field.Path.Length - 1]), reader.GetIndexOf(field.Name)));
        }

        private InstanceMap BuildInstanceTree(ReflectedField field)
        {
            string parentPath = field.GetParentPath();
            if (string.IsNullOrWhiteSpace(parentPath)) return this.root;
            if (this.objPath.TryGetValue(parentPath, out var parent)) return parent;

            parent = this.root;

            StringBuilder nameBuilder = new StringBuilder();
            for (int i = 0; i < field.Path.Length - 1; i++)
            {
                if (nameBuilder.Length > 0)
                    nameBuilder.Append('.');

                nameBuilder.Append(field.Path[i].Name);
                parent = GetOrCreate(parent, field.Path[i], nameBuilder.ToString(), i);
            }

            return parent;
        }

        private InstanceMap GetOrCreate(InstanceMap parent, MemberInfo member, string fullName, int index)
        {
            if (this.objPath.TryGetValue(fullName, out var map)) return map;

            map = new InstanceMap(parent, member) { Index = index };
            this.objPath[fullName] = map;
            return map;
        }

        private ColumnInfo GetColumn(MemberInfo member)
        {
            if (member is PropertyInfo prop) return new ColumnInfo(this.registry, prop);
            if (member is FieldInfo field) return new ColumnInfo(this.registry, field);

            throw new NotSupportedException();
        }

        public object Read(DbDataReader reader)
        {
            this.CreateInstance();

            foreach (var item in this.columns)
                item.Set(reader[item.Index]);

            return this.root.Instance;
        }

        private void CreateInstance()
        {
            this.root.CreateInstance();

            foreach (var item in objPath.Values.OrderBy(x => x.Index))
                item.CreateInstance();
        }

        private class MappedColumn
        {
            private readonly InstanceMap owner;

            public ColumnInfo Column { get; }
            public int Index { get; }

            public MappedColumn(InstanceMap owner, ColumnInfo column, int index)
            {
                this.owner = owner;
                this.Index = index;
                this.Column = column;
            }

            public void Set(object value)
            {
                Column.Set(this.owner.Instance, value);
            }
        }

        private class InstanceMap
        {
            private readonly MemberInfo member;
            private readonly InstanceMap parent;

            public object Instance { get; private set; }
            public int Index { get; set; }
            public Type Type { get; }

            public InstanceMap(Type type)
            {
                Type = type;
            }

            public InstanceMap(InstanceMap parent, MemberInfo member)
            {
                this.parent = parent;
                this.member = member;
                Type = member is PropertyInfo prop ? prop.PropertyType : ((FieldInfo)member).FieldType;
            }

            public void CreateInstance()
            {
                Instance = Activator.CreateInstance(Type);
                Attach();
            }

            private void Attach()
            {
                if (parent == null)
                    return;

                if (member is PropertyInfo prop) prop.SetValue(parent.Instance, this.Instance);
                else ((FieldInfo)member).SetValue(parent.Instance, this.Instance);
            }

            public override string ToString()
            {
                return this.Type.ToString();
            }
        }

        public override string ToString()
        {
            return Type.Name;
        }

        public class TreeNode
        {
            public string Key { get; set; }
            public List<TreeNode> Children { get; } = new List<TreeNode>();

            public TreeNode(string key)
            {
                Key = key;
            }

            public void AddChild(TreeNode child)
            {
                Children.Add(child);
            }

            public TreeNode FindChild(string path)
            {
                if (path == null) return null;

                return this.InternalFindChild(path.Split('.'), 0);
            }

            private TreeNode InternalFindChild(string[] keys, int offset)
            {
                if (keys.Length <= offset) return null;

                string key = keys[offset];
                foreach (var node in this.Children)
                    if (keys.Length == offset + 1 && node.Key == key) return node;
                    else if (node.Key == key) return node.InternalFindChild(keys, offset + 1);

                return null;
            }

            public static TreeNode Map(Type type, string parentKey = "")
            {
                var rootNode = new TreeNode(parentKey);
                foreach (var property in type.GetProperties())
                {
                    bool isNative = TranslationUtils.IsNative(property.PropertyType, false) || ReflectionUtils.IsCollection(property.PropertyType);
                    if (isNative) rootNode.AddChild(new TreeNode(property.Name));
                    else rootNode.AddChild(Map(property.PropertyType, property.Name));
                }

                return rootNode;
            }

            public static void PrintTree(TreeNode node, int level = 0)
            {
                Console.WriteLine(new string('-', level * 2) + node.Key);
                foreach (var child in node.Children)
                    PrintTree(child, level + 1);
            }
        }
    }
}
