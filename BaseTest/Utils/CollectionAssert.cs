using Xunit;

namespace BaseTest.Utils
{
    public static class CollectionAssert
    {
        public static void Contains<T>(IEnumerable<T> collection, T expected, string? message = null)
        {
            Assert.NotNull(collection);
            if (collection.Contains(expected))
                return;

            string errorMessage = message ?? $"Expected collection to contain element {expected}, but it was not found.";
            Assert.True(false, errorMessage);
        }

        public static void Contains<T>(IEnumerable<T> collection, T expected,
            IEqualityComparer<T> comparer, string? message = null)
        {
            Assert.NotNull(collection);
            Assert.NotNull(comparer);
            if (collection.Contains(expected, comparer))
                return;

            string errorMessage = message ?? $"Expected collection to contain element {expected}, but it was not found.";
            Assert.True(false, errorMessage);
        }

        public static void Contains<T>(IEnumerable<T> collection, Func<T, bool> predicate, string? message = null)
        {
            Assert.NotNull(collection);
            Assert.NotNull(predicate);
            if (collection.Any(predicate))
                return;

            string errorMessage = message ?? "Expected collection to contain an element matching the specified criteria, but none was found.";
            Assert.True(false, errorMessage);
        }

        /// <summary>
        /// Asserts that all elements from the expected collection are present in the actual collection in any order.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collections.</typeparam>
        /// <param name="expectedCollection">The collection whose elements should all be present in the actual collection.</param>
        /// <param name="actual">The collection that should contain all elements from the expected collection.</param>
        /// <param name="message">Optional message to display on failure.</param>
        public static void ContainsAll<T>(IEnumerable<T> expectedCollection, IEnumerable<T> actual, string? message = null)
        {
            Assert.NotNull(expectedCollection);
            Assert.NotNull(actual);

            var expectedList = expectedCollection.ToList();
            var actualList = actual.ToList();

            Assert.True(expectedList.Count == actualList.Count, GetMessage($"Collections have different counts. Expected: {expectedList.Count}, Actual: {actualList.Count}", message));

            foreach (var item in expectedList)
                Assert.True(
                    actualList.Contains(item),
                    GetMessage($"Expected actual collection to contain item: {item}, but it was not found.", message));
        }

        /// <summary>
        /// Asserts that all elements from the expected collection are present in the actual collection in any order using a custom comparer.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collections.</typeparam>
        /// <param name="expectedCollection">The collection whose elements should all be present in the actual collection.</param>
        /// <param name="actual">The collection that should contain all elements from the expected collection.</param>
        /// <param name="comparer">The equality comparer to use when comparing elements.</param>
        /// <param name="message">Optional message to display on failure.</param>
        public static void ContainsAll<T>(IEnumerable<T> expectedCollection, IEnumerable<T> actual, IEqualityComparer<T> comparer, string? message = null)
        {
            Assert.NotNull(expectedCollection);
            Assert.NotNull(actual);
            Assert.NotNull(comparer);

            var expectedList = expectedCollection.ToList();
            var actualList = actual.ToList();

            Assert.True(expectedList.Count == actualList.Count, GetMessage($"Collections have different counts. Expected: {expectedList.Count}, Actual: {actualList.Count}", message));

            foreach (var item in expectedList)
                Assert.True(
                    actualList.Any(element => comparer.Equals(element, item)),
                    GetMessage($"Expected actual collection to contain item: {item}, but it was not found using the specified comparer.", message));
        }

        /// <summary>
        /// Asserts that the actual array contains all elements from the expected array in the same order and position.
        /// </summary>
        /// <typeparam name="T">The type of elements in the arrays.</typeparam>
        /// <param name="expectedCollection">The array whose elements should be present in the actual array in order.</param>
        /// <param name="actual">The array that should contain the expected array.</param>
        /// <param name="message">Optional message to display on failure.</param>
        public static void Equal<T>(IEnumerable<T> expectedCollection, IEnumerable<T> actual, string? message = null)
        {
            Assert.NotNull(expectedCollection);
            Assert.NotNull(actual);

            var expectedList = expectedCollection.ToList();
            var actualList = actual.ToList();

            Assert.True(expectedList.Count == actualList.Count, GetMessage($"Collections have different counts. Expected: {expectedList.Count}, Actual: {actualList.Count}", message));

            for (int i = 0; i < expectedList.Count; i++)
                Assert.True(
                    EqualityComparer<T>.Default.Equals(expectedList[i], actualList[i]),
                    GetMessage("Actual collection does not contain expected items in the specified order", message));
        }

        private static string GetMessage(string defaultMessage, string? message)
        {
            return string.IsNullOrEmpty(message) ? defaultMessage : message;
        }
    }
}