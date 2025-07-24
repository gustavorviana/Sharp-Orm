using System.Collections.Generic;

namespace SharpOrm.Builder.Tables
{
    public class IndexDefinition
    {
        public string[] Columns { get; }
        public string Name { get; set; }
        public string TableName { get; set; }
        public bool IsUnique { get; set; }
        public bool IsClustered { get; set; }
        public Dictionary<string, object> Annotations { get; set; }

        public IndexDefinition(string[] columnNames)
        {
            Columns = columnNames;
            Annotations = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the effective constraint name (custom or default).
        /// </summary>
        public virtual string GetEffectiveName()
        {
            return string.IsNullOrEmpty(Name) ? GetDefaultName() : Name;
        }

        public virtual string GetDefaultName()
        {
            var prefix = IsUnique ? "UX" : "IX";
            var columnList = string.Join("_", Columns);
            return $"{prefix}_{TableName}_{columnList}";
        }
    }
}
