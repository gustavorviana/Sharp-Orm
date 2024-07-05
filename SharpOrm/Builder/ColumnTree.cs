using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder
{
    public class ColumnTree
    {
        public MemberInfo[] Path { get; }
        public ColumnInfo Column { get; }
        public string ParentPah { get; }

        internal ColumnTree(ColumnInfo column, IEnumerable<MemberInfo> path)
        {
            this.Column = column;
            this.Path = path.ToArray();
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

            return builder.Append(this.Path[this.Path.Length - 1].Name).ToString();
        }
    }
}
