using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Tables
{
    /// <summary>
    /// Defines a contract for building database indexes.
    /// </summary>
    public interface IIndexBuilder
    {
        /// <summary>
        /// Sets the index name.
        /// </summary>
        /// <param name="indexName">The index name.</param>
        /// <returns>The current IIndexBuilder instance.</returns>
        IIndexBuilder HasName(string indexName);

        /// <summary>
        /// Sets the index as unique.
        /// </summary>
        /// <param name="isUnique">Whether the index is unique.</param>
        /// <returns>The current IIndexBuilder instance.</returns>
        IIndexBuilder IsUnique(bool isUnique = true);

        /// <summary>
        /// Sets the index as clustered.
        /// </summary>
        /// <param name="isClustered">Whether the index is clustered.</param>
        /// <returns>The current IIndexBuilder instance.</returns>
        IIndexBuilder IsClustered(bool isClustered = true);

        /// <summary>
        /// Adds an annotation to the index.
        /// </summary>
        /// <param name="annotation">The annotation name.</param>
        /// <param name="value">The annotation value.</param>
        /// <returns>The current IIndexBuilder instance.</returns>
        IIndexBuilder HasAnnotation(string annotation, object value);
    }
}