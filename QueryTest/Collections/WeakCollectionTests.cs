using SharpOrm.Collections;
using System.ComponentModel;

namespace QueryTest.Collections
{
    public class WeakCollectionTests
    {
        [Fact]
        public void AddAndRemoveItemTest()
        {
            var collection = new WeakComponentsRef<Component>();
            var component = new Component();

            collection.Add(component);
            Assert.Single(collection);
            Assert.Equal(0, collection.IndexOf(component));

            collection.Remove(component);
            Assert.Empty(collection);
            Assert.Equal(-1, collection.IndexOf(component));
        }

        [Fact]
        public void ClearTest()
        {
            var collection = new WeakComponentsRef<Component> { new(), new() };
            Assert.Equal(2, collection.Count);

            collection.Clear();
            Assert.Empty(collection);
        }

        [Fact]
        public void DisposedEventHandlerTest()
        {
            var component = new Component();
            var collection = new WeakComponentsRef<Component> { component };
            Assert.Single(collection);

            component.Dispose();
            Assert.Empty(collection);
        }

        [Fact]
        public void RemoveNotAliveTest()
        {
            var collection = new WeakComponentsRef<Component>();

            for (int i = 0; i < 100; i++)
                collection.Add(new());

            GC.Collect();
            GC.WaitForPendingFinalizers();

            collection.RemoveNotAlive();
            Assert.Equal(collection.AliveCount, collection.Count);
        }

        [Fact]
        public void RemoveNotAliveOnAddTest()
        {
            var collection = new WeakComponentsRef<Component>();

            for (int i = 0; i < 10; i++)
                collection.Add(new());

            GC.Collect();
            GC.WaitForPendingFinalizers();

            collection.Add(new());
            Assert.Equal(collection.AliveCount, collection.Count);
        }
    }
}
