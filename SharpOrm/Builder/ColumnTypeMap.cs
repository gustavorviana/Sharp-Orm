using System;
using System.Data;

namespace SharpOrm.Builder
{
    public abstract class ColumnTypeMap
    {
        public abstract bool CanWork(Type type);

        public abstract string GetTypeString(DataColumn column);
    }
}
