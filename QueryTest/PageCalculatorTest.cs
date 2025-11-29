using SharpOrm;

namespace QueryTest
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

        [Fact]
        public void CalcPages_WithZeroPeerPage_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => PageCalculator.CalcPages(100, 0));
            Assert.Equal("peerPage", ex.ParamName);
        }

        [Fact]
        public void CalcPages_WithNegativePeerPage_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => PageCalculator.CalcPages(100, -1));
            Assert.Equal("peerPage", ex.ParamName);
        }

        [Fact]
        public void CalcPages_WithZeroSize_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            var value = PageCalculator.CalcPages(0, 10);
            Assert.Equal(0, value);
        }

        [Fact]
        public void CalcPages_WithNegativeSize_ShouldThrowArgumentOutOfRangeException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => PageCalculator.CalcPages(-1, 10));
            Assert.Equal("size", ex.ParamName);
        }

        [Fact]
        public void CalcPages_WithValidValues_ShouldReturnCorrectPages()
        {
            // Act
            var pages = PageCalculator.CalcPages(100, 10);

            // Assert
            Assert.Equal(10, pages);
        }

        [Fact]
        public void GetStartIndex_WithValidValues_ShouldReturnCorrectIndex()
        {
            // Arrange
            var calculator = new PageCalculator(100, 10);

            // Act & Assert
            Assert.Equal(0, calculator.GetStartIndex(1));
            Assert.Equal(10, calculator.GetStartIndex(2));
            Assert.Equal(90, calculator.GetStartIndex(10));
        }
    }
}
