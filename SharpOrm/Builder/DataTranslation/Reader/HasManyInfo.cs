using System;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    internal class HasManyInfo : ForeignInfo
    {
        public string LocalKey { get; }

        public HasManyInfo(Type type, object foreignKey, int depth, string localKey) : base(type, foreignKey, depth)
        {
            this.LocalKey = localKey;
        }

        public HasManyInfo(LambdaColumn column, object foreignKey, string localKey) : base(column, foreignKey)
        {
            this.LocalKey = localKey;
        }
    }
}
