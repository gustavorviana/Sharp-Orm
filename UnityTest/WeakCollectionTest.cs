using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm.Collections;
using System;
using System.ComponentModel;

namespace UnityTest
{
    [TestClass]
    public class WeakCollectionTest
    {
        [TestMethod]
        public void AddAndRemoveItemTest()
        {
            var collection = new WeakComponentsRef<Component>();
            var component = new Component();

            collection.Add(component);
            Assert.AreEqual(1, collection.Count);
            Assert.AreEqual(0, collection.IndexOf(component));

            collection.Remove(component);
            Assert.AreEqual(0, collection.Count);
            Assert.AreEqual(-1, collection.IndexOf(component));
        }

        [TestMethod]
        public void ClearTest()
        {
            var collection = new WeakComponentsRef<Component> { new(), new() };
            Assert.AreEqual(2, collection.Count);

            collection.Clear();
            Assert.AreEqual(0, collection.Count);
        }

        [TestMethod]
        public void DisposedEventHandlerTest()
        {
            var component = new Component();
            var collection = new WeakComponentsRef<Component> { component };
            Assert.AreEqual(1, collection.Count);

            component.Dispose();
            Assert.AreEqual(0, collection.Count);
        }

        [TestMethod]
        public void RemoveNotAliveTest()
        {
            var collection = new WeakComponentsRef<Component>();

            for (int i = 0; i < 100; i++)
                collection.Add(new());

            GC.Collect();
            GC.WaitForPendingFinalizers();

            collection.RemoveNotAlive();
            Assert.AreEqual(collection.AliveCount, collection.Count);
        }

        [TestMethod]
        public void RemoveNotAliveOnAddTest()
        {
            var collection = new WeakComponentsRef<Component>();

            for (int i = 0; i < 10; i++)
                collection.Add(new());

            GC.Collect();
            GC.WaitForPendingFinalizers();

            collection.Add(new());
            Assert.AreEqual(collection.AliveCount, collection.Count);
        }
    }
}
