using SharpOrm.DataTranslation;

namespace SharpOrm.Builder
{
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