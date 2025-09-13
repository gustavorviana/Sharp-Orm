namespace SharpOrm.Fb
{
    public static class FbIndexNotations
    {
        // Anotações comuns (MySQL e Firebird)
        public const string Comment = "Comment";

        // Anotações específicas do MySQL
        public const string IndexType = "IndexType";
        public const string KeyBlockSize = "KeyBlockSize";

        // Anotações específicas do Firebird
        public const string SortOrder = "SortOrder";
        public const string ComputedBy = "ComputedBy";
    }
}
