namespace SharpOrm.DataTranslation.Reader.NameLoader
{
    internal class WithoutColumnNameLoader : IColumnNameLoader
    {
        public string Prefix => null;

        public string Get(string name) => name;
    }
}
