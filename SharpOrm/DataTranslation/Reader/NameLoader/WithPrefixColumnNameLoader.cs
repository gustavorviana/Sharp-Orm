namespace SharpOrm.DataTranslation.Reader.NameLoader
{
    internal class WithPrefixColumnNameLoader : IColumnNameLoader
    {
        public string Prefix { get; }

        public WithPrefixColumnNameLoader(string prefix)
        {
            Prefix = prefix;
        }

        public string Get(string name)
        {
            return $"{Prefix}_{name}";
        }
    }
}
