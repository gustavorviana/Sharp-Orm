using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnityTest
{
    [TestClass]
    public class PageCalculatorTest
    {
        private static readonly PageCalculator calculator = new PageCalculator(119, 5);

        [TestMethod]
        public void GetPages()
        {
            Assert.AreEqual(24, calculator.Pages);
        }

        [TestMethod]
        public void GetFirstPageStartIndex()
        {
            Assert.AreEqual(0, calculator.GetStartIndex(1));
        }

        [TestMethod]
        public void GetSecondPageStartIndex()
        {
            Assert.AreEqual(5, calculator.GetStartIndex(2));
        }

        [TestMethod]
        public void GetLastPageStartIndex()
        {
            Assert.AreEqual(115, calculator.GetStartIndex((int)calculator.Pages));
        }
    }
}
