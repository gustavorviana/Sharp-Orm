using System;
using System.Linq;
using System.Collections.Generic;

namespace SharpOrm
{
    public abstract class Model
    {
        internal readonly Dictionary<string, object> columns = new Dictionary<string, object>();

        protected virtual internal Cell[] GetCells()
        {
            return this.columns.Select(col => new Cell(col.Key, col.Value)).ToArray();
        }

        protected T GetOrDefault<T>(string name, T defValue = default) where T : Enum
        {
            object value = this.GetRawOrDefault(name, defValue);
            if (value is T tVal)
                return tVal;

            return (T)Enum.ToObject(typeof(T), value);
        }

        protected T GetValue<T>(string name, T defValue = default) where T : struct
        {
            if (!typeof(T).IsPrimitive && typeof(T) != typeof(DateTime) && typeof(T) != typeof(TimeSpan))
                throw new NotSupportedException("Only primitive types are supported");

            return (T)this.GetRawOrDefault(name, defValue);
        }

        protected string GetStringValue(string name, string defValue = default)
        {
            return this.GetRawOrDefault(name, defValue)?.ToString();
        }

        protected object GetRawOrDefault(string name, object defValue)
        {
            name = name.ToLower();

            foreach (var keyValue in this.columns)
                if (keyValue.Key.ToLower() == name)
                    return keyValue.Value;

            return defValue;
        }
    }
}
