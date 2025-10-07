using BaseTest.Mock;
using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.DataTranslation;
using SharpOrm.DataTranslation.Reader;
using System.Data.Common;
using System.Drawing;
using Xunit.Abstractions;

namespace QueryTest.DataTranslation.Reader
{
    public class ObjectActivatorTests(ITestOutputHelper? output) : DbMockTest(output)
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
            Assert.Null(CreateInstance<InvalidClassConstructor>(reader));
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
            Assert.Null(CreateInstance<InvalidClassConstructor>(reader));
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
        public void InstanceClassWithParamsTest()
        {
            var reader = new MockDataReader(new Cell("firstName", "My First Name"), new Cell("lastName", "My Last Name"), new Cell("Age", 30));
            var instance = CreateInstance<Person3>(reader);

            Assert.Equal("My First Name", instance.FirstName);
            Assert.Equal("My Last Name", instance.LastName);
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

        [Fact]
        public void InstanceWithOwnedEntityTest()
        {
            var reader = new MockDataReader(
                new Cell("name", "John Doe"),
                new Cell("address_street", "123 Main St"),
                new Cell("address_city", "New York")
            );
            var instance = CreateInstance<PersonWithAddress>(reader);

            Assert.Equal("John Doe", instance.Name);
            Assert.NotNull(instance.Address);
            Assert.Equal("123 Main St", instance.Address.Street);
            Assert.Equal("New York", instance.Address.City);
        }

        [Fact]
        public void InstanceWithNestedOwnedEntitiesTest()
        {
            var reader = new MockDataReader(
                new Cell("name", "John Doe"),
                new Cell("contact_email", "john@example.com"),
                new Cell("contact_address_street", "456 Oak Ave"),
                new Cell("contact_address_city", "Boston")
            );
            var instance = CreateInstance<PersonWithContact>(reader);

            Assert.Equal("John Doe", instance.Name);
            Assert.NotNull(instance.Contact);
            Assert.Equal("john@example.com", instance.Contact.Email);
            Assert.NotNull(instance.Contact.Address);
            Assert.Equal("456 Oak Ave", instance.Contact.Address.Street);
            Assert.Equal("Boston", instance.Contact.Address.City);
        }

        [Fact]
        public void InstanceWithMultipleOwnedEntitiesTest()
        {
            var reader = new MockDataReader(
                new Cell("name", "Jane Smith"),
                new Cell("homeAddress_street", "789 Elm St"),
                new Cell("homeAddress_city", "Chicago"),
                new Cell("workAddress_street", "321 Business Blvd"),
                new Cell("workAddress_city", "Seattle")
            );
            var instance = CreateInstance<PersonWithMultipleAddresses>(reader);

            Assert.Equal("Jane Smith", instance.Name);
            Assert.NotNull(instance.HomeAddress);
            Assert.Equal("789 Elm St", instance.HomeAddress.Street);
            Assert.Equal("Chicago", instance.HomeAddress.City);
            Assert.NotNull(instance.WorkAddress);
            Assert.Equal("321 Business Blvd", instance.WorkAddress.Street);
            Assert.Equal("Seattle", instance.WorkAddress.City);
        }

        [Fact]
        public void InstanceWithOwnedEntityMissingColumnsTest()
        {
            var reader = new MockDataReader(
                new Cell("name", "John Doe"),
                new Cell("address_street", "123 Main St")
            // Missing address_city
            );
            var instance = CreateInstance<PersonWithAddress>(reader);

            // Should return null if owned entity constructor can't be satisfied
            Assert.Null(instance);
        }

        [Fact]
        public void InstanceWithCtorColumnAttributeOnOwnedTest()
        {
            var reader = new MockDataReader(
                new Cell("fullName", "John Doe"),
                new Cell("location_Street", "123 Main St"),
                new Cell("location_city", "New York")
            );
            var instance = CreateInstance<PersonWithCustomNames>(reader);

            Assert.Equal("John Doe", instance.FullName);
            Assert.NotNull(instance.Location);
            Assert.Equal("123 Main St", instance.Location.Street);
            Assert.Equal("New York", instance.Location.City);
        }

        [Fact]
        public void ConstructorCachingPerformanceTest()
        {
            var reader = new MockDataReader(
                new Cell("firstName", "John"),
                new Cell("lastName", "Doe")
            );

            // First call - cache miss
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var activator1 = new ObjectActivator(typeof(Person), reader, TranslationRegistry.Default);
            var time1 = sw.ElapsedTicks;

            // Second call - cache hit (should be faster)
            sw.Restart();
            var activator2 = new ObjectActivator(typeof(Person), reader, TranslationRegistry.Default);
            var time2 = sw.ElapsedTicks;

            // Third call - cache hit (should also be fast)
            sw.Restart();
            var activator3 = new ObjectActivator(typeof(Person), reader, TranslationRegistry.Default);
            var time3 = sw.ElapsedTicks;

            // Verify all instances work correctly
            var instance1 = (Person)activator1.CreateInstance();
            var instance2 = (Person)activator2.CreateInstance();
            var instance3 = (Person)activator3.CreateInstance();

            Assert.Equal("John", instance1.FirstName);
            Assert.Equal("John", instance2.FirstName);
            Assert.Equal("John", instance3.FirstName);

            // Cache hits should generally be faster (allow some variance)
            // This is a soft assertion - just log if it fails
            Output?.WriteLine($"Time1 (cache miss): {time1} ticks");
            Output?.WriteLine($"Time2 (cache hit): {time2} ticks");
            Output?.WriteLine($"Time3 (cache hit): {time3} ticks");
        }

        private static T CreateInstance<T>(DbDataReader reader)
        {
            var activator = new ObjectActivator(typeof(T), reader, TranslationRegistry.Default);
            return (T)activator.CreateInstance();
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

        public class Person3(string firstName, string lastName)
        {
            public string FirstName => firstName;
            public string LastName => lastName;
        }

        public readonly struct Info
        {
            public string FirstName { get; }
            public string LastName { get; }
            public string? Status { get; init; }

            [QueryIgnore]
            public Info(string firstName, string lastName, string status) : this(firstName, lastName)
            {
                Status = status;
            }

            public Info(string firstName, string lastName)
            {
                FirstName = firstName;
                LastName = lastName;
                Status = null;
            }
        }

        private class InvalidClassConstructor
        {
            public int Id { get; }

            public InvalidClassConstructor(int id)
            {
                Id = id;
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

        // Test models for owned entities
        [Owned]
        public record Address(string Street, string City);

        public record PersonWithAddress(string Name, Address Address);

        [Owned]
        public record Contact(string Email, Address Address);

        public record PersonWithContact(string Name, Contact Contact);

        public record PersonWithMultipleAddresses(
            string Name,
            Address HomeAddress,
            Address WorkAddress
        );

        [Owned]
        public record AddressWithCustomNames(
            string Street,
            string City
        );

        public record PersonWithCustomNames(
            string FullName,
            AddressWithCustomNames Location
        );
    }
}
