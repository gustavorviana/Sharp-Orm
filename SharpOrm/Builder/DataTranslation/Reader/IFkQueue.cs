using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    public interface IFkQueue
    {
        void EnqueueForeign(object owner, object fkValue, ColumnInfo column);
    }
}
