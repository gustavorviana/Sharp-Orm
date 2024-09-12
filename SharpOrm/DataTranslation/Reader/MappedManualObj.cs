using SharpOrm.Builder;
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
    internal class MappedManualObj : IMappedObject
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

        public static MappedManualObj FromMap<T>(TableMap<T> map, DbDataReader reader)
        {
            return new MappedManualObj(typeof(T), map.GetFields(), map.Registry, reader);
        }

        internal MappedManualObj(TableInfo table, TranslationRegistry registry, DbDataReader reader) : this(table.Type, (IEnumerable<ColumnTreeInfo>)table.Columns, registry, reader)
        {

        }

        private MappedManualObj(Type type, IEnumerable<ColumnTreeInfo> columns, TranslationRegistry registry, DbDataReader reader)
        {
            root = new InstanceMap(type);
            this.registry = registry;

            foreach (var column in columns)
                this.Map(column, reader);
        }

        private void Map(ColumnTreeInfo column, DbDataReader reader)
        {
            this.columns.Add(new MappedColumn(this.BuildInstanceTree(column), column, reader.GetIndexOf(column.Name)));
        }

        private InstanceMap BuildInstanceTree(ColumnTreeInfo field)
        {
            if (string.IsNullOrWhiteSpace(field.ParentPath)) return this.root;
            if (this.objPath.TryGetValue(field.ParentPath, out var parent)) return parent;

            parent = this.root;

            StringBuilder nameBuilder = new StringBuilder();
            for (int i = 0; i < field.Path.Length; i++)
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

            map = new InstanceMap(parent, member);
            this.objPath[fullName] = map;
            return map;
        }

        public object Read(DbDataReader reader)
        {
            this.CreateInstance();

            foreach (var item in this.columns)
                if (item.Index >= 0)
                    item.Set(reader[item.Index]);

            return this.root.Instance;
        }

        private void CreateInstance()
        {
            this.root.CreateInstance();

            foreach (var item in objPath.Values)
                item.CreateInstance();
        }

        private class MappedColumn
        {
            private readonly InstanceMap owner;

            public ColumnTreeInfo Column { get; }
            public int Index { get; }

            public MappedColumn(InstanceMap owner, ColumnTreeInfo column, int index)
            {
                this.owner = owner;
                this.Index = index;
                this.Column = column;
            }

            public void Set(object value)
            {
                Column.InternalSet(this.owner.Instance, value);
            }
        }

        private class InstanceMap
        {
            private readonly MemberInfo member;
            private readonly InstanceMap parent;

            public object Instance { get; private set; }
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
    }
}
