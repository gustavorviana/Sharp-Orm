using SharpOrm.DataTranslation;
using System;
using System.Linq.Expressions;

namespace SharpOrm.Builder
{
    public interface IModelMapper<T>
    {
        IModelMapper<T> HasKey(Expression<Func<T, object>> expression);
        IModelMapper<T> HasTimeStamps(string createdAtColumn, string updatedAtColumn);
        IModelMapper<T> MapNested(Expression<Func<T, object>> expression, string prefix = null, bool subNested = false);
        ColumnMapInfo Property(Expression<Func<T, object>> expression);
        ColumnMapInfo Property(Expression<Func<T, object>> expression, string columnName);
        IModelMapper<T> SoftDelete(string column, string dateColumn = null);
    }

    /// <summary>
    /// Interface that defines the mapping between a .NET type and a database table.
    /// </summary>
    public interface IModelMapper
    {
        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the translation registry used for data conversions.
        /// </summary>
        TranslationRegistry Registry { get; }

        /// <summary>
        /// Builds and returns the table mapping information.
        /// </summary>
        /// <returns>A <see cref="TableInfo"/> representing the mapped table.</returns>
        TableInfo Build();
    }
}