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

        public ReflectedField(TranslationRegistry registry, string name, IEnumerable<MemberInfo> path)
        {
            this.Name = name;
            this.Path = path.ToArray();
            var member = this.Path[this.Path.Length - 1];

            if (member is PropertyInfo pi) this.Column = new ColumnInfo(registry, pi);
            else if (member is FieldInfo fi) this.Column = new ColumnInfo(registry, fi);
            else throw new NotSupportedException();
        }

        public string GetParentPath()
        {
            if (this.Path.Length == 1) return string.Empty;

            StringBuilder b = new StringBuilder(this.Path[0].Name);

            for (int i = 1; i < this.Path.Length - 1; i++)
                b.Append('.').Append(this.Path[i].Name);

            return b.ToString();
        }

        public override string ToString()
        {
            return string.Concat(this.Column.Type.FullName, " ", this.GetParentPath());
        }
    }
}
