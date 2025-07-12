namespace SharpOrm.Builder.Tables
{
    internal class IndexBuilder : IIndexBuilder
    {
        public IndexDefinition Definition { get; }

        public IndexBuilder(string[] columnNames)
        {
            Definition = new IndexDefinition(columnNames);
        }

        public IIndexBuilder HasName(string indexName)
        {
            Definition.Name = indexName;
            return this;
        }

        public IIndexBuilder IsUnique(bool isUnique = true)
        {
            Definition.IsUnique = isUnique;
            return this;
        }

        public IIndexBuilder IsClustered(bool isClustered = true)
        {
            Definition.IsClustered = isClustered;
            return this;
        }

        public IIndexBuilder HasAnnotation(string annotation, object value)
        {
            Definition.Annotations[annotation] = value;
            return this;
        }
    }
}
