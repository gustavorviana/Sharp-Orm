using System.Data;

namespace SharpOrm.Builder
{
    public static class DataColumnExtension
    {
        public static bool TryGet(this PropertyCollection collection, object key, out object value)
        {
            value = null;

            if (!collection.ContainsKey(key))
                return false;

            value = collection[key];
            return true;
        }
    }
}
