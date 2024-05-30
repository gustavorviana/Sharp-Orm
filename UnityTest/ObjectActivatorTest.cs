using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder.DataTranslation.Reader;
using System;
using System.Data.Common;
using System.Drawing;
using UnityTest.Models;
using UnityTest.Utils;
using UnityTest.Utils.Mock;

namespace UnityTest
{
    [TestClass]
    public class ObjectActivatorTest : MockTest
    {
        [TestMethod]
        public void InstanceClassInstanceTest()
        {
            var reader = new MockDataReader();
            Assert.IsNotNull(CreateInstance<Customer>(reader));
        }

        [TestMethod]
        public void InstanceInvalidClassTest()
        {
            var reader = new MockDataReader();
            Assert.ThrowsException<NotSupportedException>(() => CreateInstance<InvalidClassConstructor>(reader));
        }

        [TestMethod]
        public void InstanceRecordInstanceTest()
        {
            var reader = new MockDataReader(new Cell("firstName", "My First Name"), new Cell("lastName", "My Last Name"));
            var instance = CreateInstance<Person>(reader);

            Assert.AreEqual("My First Name", instance.FirstName);
            Assert.AreEqual("My Last Name", instance.LastName);
        }

        [TestMethod]
        public void InstanceInvalidRecordInstanceTest()
        {
            var reader = new MockDataReader(new Cell("firstName", "My First Name"));
            Assert.ThrowsException<NotSupportedException>(() => CreateInstance<Person>(reader));
        }

        [TestMethod]
        public void InstanceStructTest()
        {
            var reader = new MockDataReader(new Cell("x", 1), new Cell("y", 2));
            var instance = CreateInstance<Point>(reader);

            Assert.AreEqual(1, instance.X);
            Assert.AreEqual(2, instance.Y);
        }

        [TestMethod]
        public void InstanceMultipleRecordConstructorsTest()
        {
            var reader = new MockDataReader(new Cell("firstName", "My First Name"), new Cell("lastName", "My Last Name"), new Cell("Age", 30));
            var instance = CreateInstance<Person2>(reader);

            Assert.AreEqual("My First Name", instance.FirstName);
            Assert.AreEqual("My Last Name", instance.LastName);
            Assert.AreEqual(30, instance.Age);

            reader = new MockDataReader(new Cell("firstName", "My First Name"), new Cell("lastName", "My Last Name"));
            instance = CreateInstance<Person2>(reader);

            Assert.AreEqual("My First Name", instance.FirstName);
            Assert.AreEqual("My Last Name", instance.LastName);
            Assert.AreEqual(20, instance.Age);
        }

        [TestMethod]
        public void InstanceIgnoringConstructorTest()
        {
            var reader = new MockDataReader(new Cell("firstName", "My First Name"), new Cell("lastName", "My Last Name"), new Cell("Status", "Active"));
            var instance = CreateInstance<Info>(reader);

            Assert.AreEqual("My First Name", instance.FirstName);
            Assert.AreEqual("My Last Name", instance.LastName);
            Assert.IsNull(instance.Status);
        }

        private static T CreateInstance<T>(DbDataReader reader)
        {
            var activator = new ObjectActivator(typeof(T), reader);
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

            public string Status { get; init; }
        }

        private class InvalidClassConstructor
        {
            public int Id { get; }

            public InvalidClassConstructor(int id)
            {
                this.Id = id;
            }
        }
    }
}
