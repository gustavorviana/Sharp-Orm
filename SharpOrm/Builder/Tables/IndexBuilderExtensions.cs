namespace SharpOrm.Builder.Tables
{
    public static class IndexBuilderExtensions
    {
        public static IIndexBuilder HasFillFactor(this IIndexBuilder builder, int fillFactor)
        {
            return builder.HasAnnotation(IndexAnnotations.FillFactor, fillFactor);
        }

        public static IIndexBuilder IncludeColumns(this IIndexBuilder builder, params string[] columns)
        {
            return builder.HasAnnotation(IndexAnnotations.IncludedColumns, columns);
        }

        public static IIndexBuilder HasFilter(this IIndexBuilder builder, string filter)
        {
            return builder.HasAnnotation(IndexAnnotations.Filter, filter);
        }

        public static IIndexBuilder IsOnline(this IIndexBuilder builder, bool online = true)
        {
            return builder.HasAnnotation(IndexAnnotations.Online, online);
        }

        public static IIndexBuilder OnFilegroup(this IIndexBuilder builder, string filegroup)
        {
            return builder.HasAnnotation(IndexAnnotations.Filegroup, filegroup);
        }

        public static IIndexBuilder HasIndexType(this IIndexBuilder builder, string indexType)
        {
            return builder.HasAnnotation(IndexAnnotations.IndexType, indexType);
        }

        public static IIndexBuilder HasComment(this IIndexBuilder builder, string comment)
        {
            return builder.HasAnnotation(IndexAnnotations.Comment, comment);
        }
    }
}
