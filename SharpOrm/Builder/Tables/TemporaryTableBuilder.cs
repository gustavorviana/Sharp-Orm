using System;

namespace SharpOrm.Builder.Tables
{
    public class TemporaryTableBuilder : TableBuilder
    {
        protected override bool Temporary => true;

        protected override string GetTableName()
        {
            return Guid.NewGuid().ToString("N") + "_" + base.GetTableName();
        }
    }
}
