using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder
{
    internal class ColumnTreeInfo : ColumnInfo
    {
        internal new MemberInfo column => base.column;

        internal MemberInfo[] Path { get; }
        internal string ParentPath { get; }

        internal ColumnTreeInfo(List<MemberInfo> path, IColumnInfo map, TranslationRegistry registry) : base(path.Last(), map, registry)
        {
            this.Path = path.Take(path.Count - 1).ToArray();
            this.ParentPath = GetParentPath();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(this.Type.FullName);
            builder.Append(' ').Append(this.ParentPath);

            if (!string.IsNullOrEmpty(this.ParentPath))
                builder.Append('.');

            return builder.Append(this.Path[this.Path.Length - 1].Name).ToString();
        }

        private string GetParentPath()
        {
            if (this.Path.Length == 0) return string.Empty;

            StringBuilder b = new StringBuilder(this.Path[0].Name);

            for (int i = 1; i < this.Path.Length - 1; i++)
                b.Append('.').Append(this.Path[i].Name);

            return b.ToString();
        }

        public override object GetRaw(object owner)
        {
            owner = GetValidOwner(owner);
            if (owner == null) return null;

            return base.GetRaw(owner);
        }

        private object GetValidOwner(object rootOwner)
        {
            for (int i = 0; i < this.Path.Length; i++)
                if ((rootOwner = ReflectionUtils.GetMemberValue(this.Path[i], rootOwner)) == null)
                    return null;

            return rootOwner;
        }

        internal void InternalSet(object owner, object value)
        {
            base.Set(owner, value);
        }
    }
}
