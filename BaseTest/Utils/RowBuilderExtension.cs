using SharpOrm;
using SharpOrm.DataTranslation;

namespace BaseTest.Utils
{
    public static class RowBuilderExtension
    {
        public static RowBuilder AddObject(this RowBuilder builder, object obj, bool readPk = true, bool readFk = false, TranslationRegistry? registry = null)
        {
            return builder.Add(Row.Parse(obj, obj.GetType(), readPk, readFk, registry));
        }

        public static RowBuilder AddObject(this RowBuilder builder, string prefix, object obj, bool readPk = true, bool readFk = false, TranslationRegistry? registry = null)
        {
            return builder.Add(prefix, Row.Parse(obj, obj.GetType(), readPk, readFk, registry));
        }
    }
}
