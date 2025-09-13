using System;
using System.Collections.Generic;

namespace SharpOrm.Builder
{
    internal class Metadata : IMetadata
    {
        private readonly IDictionary<string, object> _metadata = new Dictionary<string, object>();
        private bool _readonly = false;

        public bool HasKey(string key)
        {
            return _metadata.ContainsKey(key);
        }

        public IMetadata Add(string name, object value)
        {
            if (_readonly)
                throw new InvalidOperationException("Cannot add annotation: Metadata is readonly.");

            if (_metadata.ContainsKey(name))
                throw new ArgumentException($"The metadata key '{name}' has already been added.", nameof(name));

            _metadata.Add(name, value);
            return this;
        }

        public bool TryGetKey(string key, out object value)
        {
            return _metadata.TryGetValue(key, out value);
        }

        public IMetadata MakeReadonly()
        {
            _readonly = true;
            return this;
        }

        public IMetadata Clone()
        {
            var metadata = new Metadata();

            foreach (var item in _metadata)
                metadata._metadata.Add(item);

            return metadata;
        }

        object ICloneable.Clone() => Clone();

        public T GetOrDefault<T>(string key)
        {
            if (TryGetKey(key, out object value) && value is T tValue)
                return tValue;

            return default;
        }
    }
}
