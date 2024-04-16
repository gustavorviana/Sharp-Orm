using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Operators
{
    public class Min : Column
    {
        public Min(string column) : base(new SqlExpression($"MIN({column})"))
        {
        }

        public Min(string column, string alias) : base(new SqlExpression($"MIN({column}) {alias}"))
        {
        }
    }
}
