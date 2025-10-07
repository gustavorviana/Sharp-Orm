using System.Data;
using System.Reflection;

namespace SharpOrm.DataTranslation.Reader.Activator
{
    internal class ParamInfo : IParamInfo
    {
        public readonly ISqlTranslation _translation;
        private readonly ParameterInfo _parameter;
        private readonly IDataRecord _record;
        private readonly int _index;

        public ParamInfo(ParameterInfo parameter, IDataRecord record, int index, ISqlTranslation translation)
        {
            _index = index;
            _record = record;
            _parameter = parameter;
            _translation = translation;
        }

        public virtual object GetValue()
        {
            if (_index < 0)
                return _parameter.DefaultValue;

            return _translation.FromSqlValue(_record[_index], _parameter.ParameterType);
        }
    }
}
