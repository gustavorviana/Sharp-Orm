using BaseTest.Mock;
using BaseTest.Models;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.DataTranslation;
using SharpOrm.DataTranslation.Reader;
using System.Data.Common;
using System.Drawing;
using Xunit.Abstractions;

namespace QueryTest
{
    public class ObjectActivatorTest(ITestOutputHelper? output) : MockTest(output)
    {
        [Fact]
        public void InstanceClassInstanceTest()
        {
            var reader = new MockDataReader();
            Assert.NotNull(CreateInstance<Customer>(reader));
        }

        [Fact]
        public void InstanceInvalidClassTest()
        {
            var reader = new MockDataReader();
            Assert.Throws<NotSupportedException>(() => CreateInstance<InvalidClassConstructor>(reader));
        }

        [Fact]
        public void InstanceRecordInstanceTest()
        {
            var reader = new MockDataReader(new Cell("firstName", "My First Name"), new Cell("lastName", "My Last Name"));
            var instance = CreateInstance<Person>(reader);

            Assert.Equal("My First Name", instance.FirstName);
            Assert.Equal("My Last Name", instance.LastName);
        }

        [Fact]
        public void InstanceInvalidRecordInstanceTest()
        {
            var reader = new MockDataReader(new Cell("firstName", "My First Name"));
            Assert.Throws<NotSupportedException>(() => CreateInstance<Person>(reader));
        }

        [Fact]
        public void InstanceStructTest()
        {
            var reader = new MockDataReader(new Cell("x", 1), new Cell("y", 2));
            var instance = CreateInstance<Point>(reader);

            Assert.Equal(1, instance.X);
            Assert.Equal(2, instance.Y);
        }

        [Fact]
        public void InstanceMultipleRecordConstructorsTest()
        {
            var reader = new MockDataReader(new Cell("firstName", "My First Name"), new Cell("lastName", "My Last Name"), new Cell("Age", 30));
            var instance = CreateInstance<Person2>(reader);

            Assert.Equal("My First Name", instance.FirstName);
            Assert.Equal("My Last Name", instance.LastName);
            Assert.Equal(30, instance.Age);

            reader = new MockDataReader(new Cell("firstName", "My First Name"), new Cell("lastName", "My Last Name"));
            instance = CreateInstance<Person2>(reader);

            Assert.Equal("My First Name", instance.FirstName);
            Assert.Equal("My Last Name", instance.LastName);
            Assert.Equal(20, instance.Age);
        }

        [Fact]
        public void InstanceIgnoringConstructorTest()
        {
            var reader = new MockDataReader(new Cell("firstName", "My First Name"), new Cell("lastName", "My Last Name"), new Cell("Status", "Active"));
            var instance = CreateInstance<Info>(reader);

            Assert.Equal("My First Name", instance.FirstName);
            Assert.Equal("My Last Name", instance.LastName);
            Assert.Null(instance.Status);
        }

        private static T CreateInstance<T>(DbDataReader reader)
        {
            var activator = new ObjectActivator(typeof(T), reader, TranslationRegistry.Default);
            return (T)activator.CreateInstance(reader);
        }

        public record Person(string FirstName, string LastName)
        {
        }

        public record Person2(string FirstName, string LastName, int Age) : Person(FirstName, LastName)
        {
            public Person2(string FirstName, string LastName) : this(FirstName, LastName, 20)
            {

            }
        }

        public readonly struct Info
        {
            public string FirstName { get; }
            public string LastName { get; }
            public string Status { get; init; }

            [QueryIgnore]
            public Info(string firstName, string lastName, string status) : this(firstName, lastName)
            {
                this.Status = status;
            }

            public Info(string firstName, string lastName)
            {
                this.FirstName = firstName;
                this.LastName = lastName;
                this.Status = null;
            }
        }

        private class InvalidClassConstructor
        {
            public int Id { get; }

            public InvalidClassConstructor(int id)
            {
                this.Id = id;
            }
        }

        public class MyClass
        {
            public Level1 Level1 { get; set; }
            public DateTime Date { get; set; }
            public string Prop1 { get; set; }
        }

        public class Level1
        {
            public Level2 Level2 { get; set; }
            public int Id { get; set; }
        }

        public class Level2
        {
            public Level3 Level3 { get; set; }
        }

        public class Level3
        {
            public string MyLevelName { get; set; }
        }
    }
}
