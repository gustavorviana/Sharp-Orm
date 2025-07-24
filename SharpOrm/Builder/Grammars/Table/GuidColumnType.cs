using SharpOrm.DataTranslation;
using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Table
{
    public class GuidColumnType : IColumnTypeMap
    {
        private readonly TranslationRegistry _registry;
        private readonly string _type;

        public GuidColumnType(TranslationRegistry registry, string type)
        {
            _registry = registry;
            _type = type;
        }

        public bool CanWork(Type type) => type == typeof(Guid);

        public string Build(DataColumn column) => $"{_type}({GetGuidSize()})";

        protected int GetGuidSize()
        {
            switch (_registry.GuidFormat)
            {
                case "N": return 32;
                case "D": return 36;
                case "B":
                case "P": return 38;
                default: return 68;
            }
        }
    }
}
