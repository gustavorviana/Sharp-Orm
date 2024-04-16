using System;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    internal class HasManyInfo : ForeignInfo
    {
        public string LocalKey { get; }

        public HasManyInfo(LambdaColumn column, object foreignKey, string localKey) : base(column, foreignKey)
        {
            this.LocalKey = localKey;
        }
    }
}
