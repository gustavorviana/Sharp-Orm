namespace SharpOrm.DataTranslation.Reader.NameLoader
{
    internal class TableParentColumnNameLoader : IColumnNameLoader
    {
        public string Prefix { get; }

        public TableParentColumnNameLoader(string prefix)
        {
            Prefix = prefix;
        }

        public string Get(string name)
        {
            return $"{Prefix}c_{name}";
        }
    }
}
