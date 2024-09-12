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
            this.ParentPath = ReflectionUtils.ToPath(path, limit: path.Count - 1);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(this.Type.FullName);
            builder.Append(' ').Append(this.ParentPath);

            if (!string.IsNullOrEmpty(this.ParentPath))
                builder.Append('.');

            return builder.Append(this.Path[this.Path.Length - 1].Name).ToString();
        }

        public override object GetRaw(object owner)
        {
            for (int i = 0; i < this.Path.Length; i++)
                if ((owner = ReflectionUtils.GetMemberValue(this.Path[i], owner)) == null)
                    return null;

            return base.GetRaw(owner);
        }

        internal void InternalSet(object owner, object value)
        {
            base.SetRaw(owner, this.Translation.FromSqlValue(value, this.GetValidValueType()));
        }

        public override void SetRaw(object owner, object value)
        {
            for (int i = 0; i < this.Path.Length; i++)
                owner = SafeGetOwner(this.Path[i], owner);

            base.SetRaw(owner, value);
        }

        private object SafeGetOwner(MemberInfo member, object owner)
        {
            if (ReflectionUtils.GetMemberValue(member, owner) is object foundOwner) return foundOwner;
            foundOwner = Activator.CreateInstance(ReflectionUtils.GetMemberType(member));
            ReflectionUtils.SetMemberValue(member, owner, foundOwner);

            return foundOwner;
        }
    }
}
