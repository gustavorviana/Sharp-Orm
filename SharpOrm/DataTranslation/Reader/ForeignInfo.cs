using System;

namespace SharpOrm.DataTranslation.Reader
{
    internal class ForeignInfo
    {
        public FkLoaders Loader { get; }
        public ForeignKeyNodeBase Node { get; }
        /// <summary>
        /// If enabled, allows the query to create an object only with its primary key when there is no depth and allows reading the id of a foreign object on insert or update.
        /// </summary>
        [Obsolete("This interface will be removed in version 4.0. To follow market standards, only the foreign key id property should be populated, not the full object.")]
        public bool LoadForeign { get; set; }

        public ForeignInfo(FkLoaders loader, ForeignKeyNodeBase node)
        {
            Loader = loader;
            Node = node;
        }
    }
}
