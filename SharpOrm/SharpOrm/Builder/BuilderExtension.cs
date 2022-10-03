namespace SharpOrm.Builder
{
    public static class BuilderExtension
    {
        public static QueryInfo GetInfo(this QueryBase qBase)
        {
            return qBase.info;
        }
    }
}
