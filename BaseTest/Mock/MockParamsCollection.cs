using System.Collections;
using System.Data.Common;

namespace BaseTest.Mock
{
    internal class MockParamsCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _params = [];
        public override int Count => _params.Count;

        public override object SyncRoot => false;

        public override int Add(object value)
        {
            if (value is not DbParameter param)
                return -1;

            _params.Add(param);

            return IndexOf(value);
        }

        public override void AddRange(Array values)
        {
            if (values is IEnumerable<DbParameter> dbParams)
                _params.AddRange(dbParams);
        }

        public override void Clear()
        {
            _params.Clear();
        }

        public override bool Contains(object value)
        {
            return value is DbParameter dp && _params.Contains(dp);
        }

        public override bool Contains(string value)
        {
            return _params.Any(c => c.ParameterName == value);
        }

        public override void CopyTo(Array array, int index)
        {
            if (array is DbParameter[] pArray)
                _params.CopyTo(pArray, index);
        }

        public override IEnumerator GetEnumerator() => _params.GetEnumerator();

        public override int IndexOf(object value)
        {
            return value is DbParameter param ? _params.IndexOf(param) : -1;
        }

        public override int IndexOf(string parameterName)
        {
            for (int i = 0; i < _params.Count; i++)
                if (_params[i].ParameterName == parameterName)
                    return i;

            return -1;
        }

        public override void Insert(int index, object value)
        {
            if (value is DbParameter param)
                _params.Insert(index, param);
        }

        public override void Remove(object value)
        {
            if (value is not DbParameter param)
                return;

            _params.Remove(param);
        }

        public override void RemoveAt(int index)
        {
            _params.RemoveAt(index);
        }

        public override void RemoveAt(string parameterName)
        {
            _params.RemoveAt(IndexOf(parameterName));
        }

        protected override DbParameter GetParameter(int index)
        {
            return _params[index];
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            return _params.FirstOrDefault(p => p.ParameterName == parameterName)!;
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            _params[index] = value;
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            _params[IndexOf(parameterName)] = value;
        }
    }
}
