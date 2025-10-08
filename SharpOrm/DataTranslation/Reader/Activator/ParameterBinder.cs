using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace SharpOrm.DataTranslation.Reader.Activator
{
    internal class ParameterBinder
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly List<IParamInfo> _params = new List<IParamInfo>();
        private readonly HashSet<string> _propertiesName;
        private readonly TranslationRegistry _registry;
        private readonly IDataRecord _record;

        public int Matches { get; private set; }
        public int OptionalCount { get; private set; }
        private readonly string _prefix;

        public ParameterBinder(Type objType, TranslationRegistry registry, IDataRecord record, string prefix)
        {
            _propertiesName = new HashSet<string>(objType.GetProperties(Flags).Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
            _registry = registry;
            _record = record;
            _prefix = prefix;
        }

        public bool FindAll(ConstructorInfo constructor)
        {
            Reset();
            var ctorParams = constructor.GetParameters();

            for (int i = 0; i < ctorParams.Length; i++)
            {
                var parameter = ctorParams[i];
                if (parameter.IsOptional)
                    OptionalCount++;

                if (!_propertiesName.Contains(parameter.Name))
                    return false;

                var paramInfo = GetInfo(parameter);
                if (paramInfo == null)
                    return false;

                _params.Add(paramInfo);
            }

            return true;
        }

        public IReadOnlyList<IParamInfo> GetParameters()
        {
            return _params.ToArray();
        }

        private void Reset()
        {
            Matches = 0;
            OptionalCount = 0;
            _params.Clear();
        }

        private IParamInfo GetInfo(ParameterInfo parameter)
        {
            var fullName = GetName(parameter);
            if (parameter.ParameterType.GetCustomAttribute<OwnedAttribute>() != null)
            {
                Matches++;
                return new OwnedParamInfo(parameter, _record, _registry, parameter.ParameterType, fullName);
            }

            var columnIndex = _record.GetIndexOf(fullName);
            if (columnIndex == -1 || !(_registry.GetFor(parameter.ParameterType) is ISqlTranslation translation))
                return null;

            Matches++;
            return new ParamInfo(parameter, _record, columnIndex, translation);
        }

        private string GetName(ParameterInfo info)
        {
            return string.IsNullOrEmpty(_prefix) ? info.Name : $"{_prefix}_{info.Name}";
        }
    }
}
