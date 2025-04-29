using SharpOrm.Builder;
using System;

namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// A SQL translator that converts zero values to null and vice versa.
    /// </summary>
    public class ZeroToNullSqlTranslator : NumericTranslation
    {
        public override object FromSqlValue(object value, Type expectedType)
        {
            if (value == null || value == DBNull.Value)
                return ReflectionUtils.GetDefault(expectedType);

            return base.FromSqlValue(value, expectedType);
        }

        public override object ToSqlValue(object value, Type type)
        {
            if (value == null || value == DBNull.Value || value.Equals(ReflectionUtils.GetDefault(type)))
                return DBNull.Value;

            return base.ToSqlValue(value, type);
        }
    }
}
