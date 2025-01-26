using BaseTest.Utils;
using SharpOrm;
using SharpOrm.DataTranslation.Reader;
using System.ComponentModel.DataAnnotations.Schema;

namespace QueryTest
{
    public class MappedObjectTest : DbMockTest
    {
        [Fact]
        public void ManualPropMapTest()
        {
            const uint id = 1;
            const string name = "Test";
            const string email = "my@email.com";
            const int addressId = 2;

            using var dbReader = GetReader(
                new Cell("Id", id),
                new Cell("Name", name),
                new Cell("Email", email),
                new Cell("address_id", addressId)
            );

            var obj = MappedObject.Read<NoAddressCustomer>(dbReader);

            Assert.Equal(id, obj.Id);
            Assert.Equal(name, obj.Name);
            Assert.Equal(email, obj.Email);
            Assert.Equal(addressId, obj.AddressId);
        }

        private class NoAddressCustomer
        {
            public uint Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }

            [Column("address_id")]
            public int? AddressId { get; set; }
        }
    }
}
