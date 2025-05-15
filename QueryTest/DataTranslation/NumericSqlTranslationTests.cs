using BaseTest.Utils;
using SharpOrm.DataTranslation;

namespace QueryTest.DataTranslation
{
    public class NumericSqlTranslationTests : DbMockTest
    {
        [Fact]
        public void NullFromSql()
        {
            var translator = new ZeroToNullSqlTranslator();

            Assert.Equal((byte)0, translator.FromSqlValue(null, typeof(byte)));
            Assert.Equal((sbyte)0, translator.FromSqlValue(null, typeof(sbyte)));
            Assert.Equal((short)0, translator.FromSqlValue(null, typeof(short)));
            Assert.Equal((ushort)0, translator.FromSqlValue(null, typeof(ushort)));
            Assert.Equal(0, translator.FromSqlValue(null, typeof(int)));
            Assert.Equal(0U, translator.FromSqlValue(null, typeof(uint)));
            Assert.Equal(0L, translator.FromSqlValue(null, typeof(long)));
            Assert.Equal(0UL, translator.FromSqlValue(null, typeof(ulong)));
            Assert.Equal(0.0f, translator.FromSqlValue(null, typeof(float)));
            Assert.Equal(0.0d, translator.FromSqlValue(null, typeof(double)));
            Assert.Equal(0.0m, translator.FromSqlValue(null, typeof(decimal)));
        }

        [Fact]
        public void ZeroToSql()
        {
            var translator = new ZeroToNullSqlTranslator();

            Assert.Equal(DBNull.Value, translator.ToSqlValue((byte)0, typeof(byte)));
            Assert.Equal(DBNull.Value, translator.ToSqlValue((sbyte)0, typeof(sbyte)));
            Assert.Equal(DBNull.Value, translator.ToSqlValue((short)0, typeof(short)));
            Assert.Equal(DBNull.Value, translator.ToSqlValue((ushort)0, typeof(ushort)));
            Assert.Equal(DBNull.Value, translator.ToSqlValue(0, typeof(int)));
            Assert.Equal(DBNull.Value, translator.ToSqlValue(0U, typeof(uint)));
            Assert.Equal(DBNull.Value, translator.ToSqlValue(0L, typeof(long)));
            Assert.Equal(DBNull.Value, translator.ToSqlValue(0UL, typeof(ulong)));
            Assert.Equal(DBNull.Value, translator.ToSqlValue(0.0f, typeof(float)));
            Assert.Equal(DBNull.Value, translator.ToSqlValue(0.0d, typeof(double)));
            Assert.Equal(DBNull.Value, translator.ToSqlValue(0.0m, typeof(decimal)));
        }

        [Fact]
        public void NonZeroToSql()
        {
            var translator = new ZeroToNullSqlTranslator();

            Assert.Equal((byte)1, translator.ToSqlValue((byte)1, typeof(byte)));
            Assert.Equal((sbyte)1, translator.ToSqlValue((sbyte)1, typeof(sbyte)));
            Assert.Equal((short)1, translator.ToSqlValue((short)1, typeof(short)));
            Assert.Equal((ushort)1, translator.ToSqlValue((ushort)1, typeof(ushort)));
            Assert.Equal(1, translator.ToSqlValue(1, typeof(int)));
            Assert.Equal(1U, translator.ToSqlValue(1U, typeof(uint)));
            Assert.Equal(1L, translator.ToSqlValue(1L, typeof(long)));
            Assert.Equal(1UL, translator.ToSqlValue(1UL, typeof(ulong)));
            Assert.Equal(1.0f, translator.ToSqlValue(1.0f, typeof(float)));
            Assert.Equal(1.0d, translator.ToSqlValue(1.0d, typeof(double)));
            Assert.Equal(1.0m, translator.ToSqlValue(1.0m, typeof(decimal)));
        }

        [Fact]
        public void NonZeroFromSql()
        {
            var translator = new ZeroToNullSqlTranslator();

            Assert.Equal((byte)1, translator.FromSqlValue(1, typeof(byte)));
            Assert.Equal((sbyte)1, translator.FromSqlValue(1, typeof(sbyte)));
            Assert.Equal((short)1, translator.FromSqlValue(1, typeof(short)));
            Assert.Equal((ushort)1, translator.FromSqlValue(1, typeof(ushort)));
            Assert.Equal(1, translator.FromSqlValue(1, typeof(int)));
            Assert.Equal(1U, translator.FromSqlValue(1U, typeof(uint)));
            Assert.Equal(1L, translator.FromSqlValue(1L, typeof(long)));
            Assert.Equal(1UL, translator.FromSqlValue(1UL, typeof(ulong)));
            Assert.Equal(1.0f, translator.FromSqlValue(1.0f, typeof(float)));
            Assert.Equal(1.0d, translator.FromSqlValue(1.0d, typeof(double)));
            Assert.Equal(1.0m, translator.FromSqlValue(1.0m, typeof(decimal)));
        }
    }
}
