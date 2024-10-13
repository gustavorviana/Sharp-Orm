using SharpOrm;

namespace UnityTest
{
    public class PageCalculatorTest
    {
        private static readonly PageCalculator calculator = new(119, 5);

        [Fact]
        public void GetPages()
        {
            Assert.Equal(24, calculator.Pages);
        }

        [Fact]
        public void GetFirstPageStartIndex()
        {
            Assert.Equal(0, calculator.GetStartIndex(1));
        }

        [Fact]
        public void GetSecondPageStartIndex()
        {
            Assert.Equal(5, calculator.GetStartIndex(2));
        }

        [Fact]
        public void GetLastPageStartIndex()
        {
            Assert.Equal(115, calculator.GetStartIndex((int)calculator.Pages));
        }
    }
}
