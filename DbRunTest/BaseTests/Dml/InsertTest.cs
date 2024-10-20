using BaseTest.Models;
using BaseTest.Utils;
using DbRunTest.Fixtures;
using SharpOrm;
using System.Data.Common;
using Xunit.Abstractions;

namespace DbRunTest.BaseTests.Dml
{
    public abstract class InsertTest<T>(ITestOutputHelper output, DbFixture<T> connection) : DmlTest<T>(output, connection), IClassFixture<UnsafeDbFixture<T>> where T : DbConnection, new()
    {
        [Fact]
        public virtual void Insert()
        {
            using var q = NewQuery(TestTableUtils.TABLE);
            q.Insert(TestTableUtils.NewRow(120, "User 1").Cells);
            int lastId = q.Insert(TestTableUtils.NewRow(null, "User 2").Cells);

            Assert.Equal(2, q.Count());
            Assert.Equal(121, lastId);
        }

        [Fact]
        public void InsertForeignObject()
        {
            const uint Id = 1;
            const string Name = "User 1";
            const string Email = "my@email.com";
            using var query = NewQuery<Customer>(this.GetUnsafeManager());

            query.Delete();

            query.Insert(new Customer
            {
                Id = Id,
                Name = Name,
                Email = Email
            });

            var customer = query.FirstOrDefault();

            Assert.NotNull(customer);
            Assert.Null(customer.Address);
            Assert.Equal(Id, customer.Id);
            Assert.Equal(Name, customer.Name);
            Assert.Equal(Email, customer.Email);
        }

        [Fact]
        public void Upsert()
        {
            using var query = NewQuery(TestTableUtils.TABLE);

            query.Upsert(TestTableUtils.NewRow(1, "A1"), [TestTableUtils.ID]);
            Assert.Equal(1, Convert.ToInt32(query.FirstRow()[TestTableUtils.ID]));

            query.Upsert(TestTableUtils.NewRow(1, "Josh"), [TestTableUtils.ID]);
            Assert.Equal("Josh", query.FirstRow()[TestTableUtils.NAME]);
        }
    }
}
