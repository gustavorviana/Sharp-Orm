using SharpOrm.Builder;

namespace SharpOrm.DataTranslation.Reader
{
    internal class HasManyInfo : ForeignInfo
    {
        public string LocalKey { get; }

        public HasManyInfo(LambdaColumn column, object foreignKey, string localKey) : base(column, foreignKey)
        {
            LocalKey = localKey;
        }
    }
}
