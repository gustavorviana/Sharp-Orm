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

            this._params.Add(param);

            return this.IndexOf(value);
        }

        public override void AddRange(Array values)
        {
            if (values is IEnumerable<DbParameter> dbParams)
                this._params.AddRange(dbParams);
        }

        public override void Clear()
        {
            this._params.Clear();
        }

        public override bool Contains(object value)
        {
            return value is DbParameter dp && this._params.Contains(dp);
        }

        public override bool Contains(string value)
        {
            return this._params.Any(c => c.ParameterName == value);
        }

        public override void CopyTo(Array array, int index)
        {
            if (array is DbParameter[] pArray)
                this._params.CopyTo(pArray, index);
        }

        public override IEnumerator GetEnumerator() => this._params.GetEnumerator();

        public override int IndexOf(object value)
        {
            return value is DbParameter param ? this._params.IndexOf(param) : -1;
        }

        public override int IndexOf(string parameterName)
        {
            for (int i = 0; i < this._params.Count; i++)
                if (this._params[i].ParameterName == parameterName)
                    return i;

            return -1;
        }

        public override void Insert(int index, object value)
        {
            if (value is DbParameter param)
                this._params.Insert(index, param);
        }

        public override void Remove(object value)
        {
            if (value is not DbParameter param)
                return;

            this._params.Remove(param);
        }

        public override void RemoveAt(int index)
        {
            this._params.RemoveAt(index);
        }

        public override void RemoveAt(string parameterName)
        {
            this._params.RemoveAt(this.IndexOf(parameterName));
        }

        protected override DbParameter GetParameter(int index)
        {
            return this._params[index];
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            return this._params.FirstOrDefault(p => p.ParameterName == parameterName)!;
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            this._params[index] = value;
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            this._params[this.IndexOf(parameterName)] = value;
        }
    }
}
