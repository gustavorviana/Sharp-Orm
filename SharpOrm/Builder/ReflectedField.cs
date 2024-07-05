using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder
{
    public class ReflectedField
    {
        public string Name { get; }
        public MemberInfo[] Path { get; }
        public ColumnInfo Column;
        public string ParentPah { get; }

        public ReflectedField(TranslationRegistry registry, string name, IEnumerable<MemberInfo> path)
        {
            this.Name = name;
            this.Path = path.ToArray();
            var member = this.Path[this.Path.Length - 1];

            if (member is PropertyInfo pi) this.Column = new ColumnInfo(registry, pi);
            else if (member is FieldInfo fi) this.Column = new ColumnInfo(registry, fi);
            else throw new NotSupportedException();

            this.ParentPah = this.GetParentPath();
        }

        private string GetParentPath()
        {
            if (this.Path.Length == 1) return string.Empty;

            StringBuilder b = new StringBuilder(this.Path[0].Name);

            for (int i = 1; i < this.Path.Length - 1; i++)
                b.Append('.').Append(this.Path[i].Name);

            return b.ToString();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(this.Column.Type.FullName);
            builder.Append(' ').Append(this.ParentPah);

            if (!string.IsNullOrEmpty(this.ParentPah))
                builder.Append('.');

            string name = this.Path[this.Path.Length - 1].Name;

            builder.Append(name);

            if (name != this.Name)
                builder.Append(" (").Append(this.Name).Append(')');

            return builder.ToString();
        }
    }
}
