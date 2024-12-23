namespace QueryTest.Interfaces
{
    public interface ISqlStringMapTest
    {
        [Fact]
        void Concat();

        [Fact]
        void Substring();

        [Fact]
        void SubstringWithColumnIndex();

        [Fact]
        void SubstringByIndexColumns();

        [Fact]
        void StringTrim();

        [Fact]
        void StringTrimStart();

        [Fact]
        void StringTrimEnd();
    }
}
