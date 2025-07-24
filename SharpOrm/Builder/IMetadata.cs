using System;

namespace SharpOrm.Builder
{
    public interface IMetadata : ICloneable
    {
        bool HasKey(string key);

        bool TryGetKey(string key, out object value);

        T GetOrDefault<T>(string key);

        IMetadata Add(string name, object value);

        new IMetadata Clone();
    }
}
