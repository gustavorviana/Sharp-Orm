using SharpOrm.Builder;
using System;
using System.Reflection;

namespace SharpOrm.DataTranslation.Reader.Activator
{
    internal class DefaultParamInfo : IParamInfo
    {
        private readonly ParameterInfo _parameter;
        public string Name => _parameter.Name;

        public DefaultParamInfo(ParameterInfo info)
        {
            _parameter = info;
        }

        public object GetValue()
        {
            if (_parameter.DefaultValue == DBNull.Value)
                return ReflectionUtils.GetDefault(_parameter.ParameterType);

            return _parameter.DefaultValue;
        }
    }
}
