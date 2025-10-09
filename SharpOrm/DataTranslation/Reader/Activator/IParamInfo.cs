namespace SharpOrm.DataTranslation.Reader.Activator
{
    internal interface IParamInfo
    {
        string Name { get; }
        object GetValue();
    }
}
