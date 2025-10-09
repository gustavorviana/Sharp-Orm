namespace SharpOrm.DataTranslation.Reader.NameLoader
{
    internal interface IColumnNameLoader
    {
        string Prefix { get; }
        string Get(string name);
    }
}
