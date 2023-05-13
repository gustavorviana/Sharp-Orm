using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm
{
    internal static class Messages
    {
        public const string InsertedTypeMismatch = "Inserted type is not the same as defined in the primary key column of the model.";
        public const string InsertValuesMismatch = "The number of inserted values is not the same number of primary keys.";
        public const string MissingPrimaryKey = "No primary key has been configured in the model.";
        public const string ColumnsNotFound = "Columns inserted to be updated were not found.";
        public const string MissingCreator = "A connection builder must have been defined.";
        public const string NoColumnsInserted = "At least one column must be inserted.";
    }
}
