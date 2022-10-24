using System;
using System.Linq;
using System.Collections.Generic;

namespace SharpOrm
{
    public abstract class Model
    {
        #region Fields

        private static readonly Type[] AllowedTypes = new Type[] { typeof(string), typeof(DateTime), typeof(TimeSpan) };

        internal readonly HashSet<string> changed = new HashSet<string>();
        internal readonly Dictionary<string, object> columns = new Dictionary<string, object>();

        public bool HasChanges => this.changed.Count > 0;
        #endregion

        #region Columns Get

        protected T GetEnumOrDefault<T>(string name, T defValue = default) where T : Enum
        {
            object value = this.GetRawOrDefault(name, defValue);
            if (value is T tVal)
                return tVal;

            return (T)Enum.ToObject(typeof(T), value);
        }

        protected T GetValueOrDefault<T>(string name, T defValue = default) where T : struct
        {
            if (!typeof(T).IsPrimitive && typeof(T) != typeof(DateTime) && typeof(T) != typeof(TimeSpan))
                throw new NotSupportedException("Only primitive types are supported");

            return (T)this.GetRawOrDefault(name, defValue);
        }

        protected string GetStringOrDefault(string name, string defValue = default)
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

        #endregion

        protected void Set(string name, object value)
        {
            name = name.ToLower();
            changed.Add(name);

            if (value == null)
            {
                this.columns[name] = null;
                return;
            }

            if (value is Enum enumVal)
            {
                this.columns[name] = Convert.ToInt32(enumVal);
                return;
            }

            Type valueType = value.GetType();
            if (!valueType.IsPrimitive && !AllowedTypes.Contains(valueType))
                throw new NotSupportedException();

            this.columns[name] = value;
        }

        protected internal virtual Cell[] GetCells()
        {
            return this.columns
                .Select(col => new Cell(col.Key, col.Value))
                .ToArray();
        }

        protected internal virtual Cell[] GetChangedCells()
        {
            return this.columns
                .Where(c => this.IsChanged(c.Key))
                .Select(kv => new Cell(kv.Key, kv.Value))
                .ToArray();
        }

        public bool IsChanged(string name)
        {
            return this.changed.Contains(name.ToLower());
        }

    }
}
