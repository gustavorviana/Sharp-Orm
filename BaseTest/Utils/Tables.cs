﻿using BaseTest.Models;
using Bogus;
using SharpOrm;

namespace BaseTest.Utils
{
    public static class Tables
    {
        public static class Order
        {
            public static Faker<Models.Order> Faker(bool withId = true, bool randomClient = true)
            {
                var faker = new Faker<Models.Order>()
                        .RuleFor(x => x.Product, f => f.Commerce.ProductName())
                        .RuleFor(x => x.Quantity, f => f.Random.Int(0, 100))
                        .RuleFor(x => x.Status, f => f.PickRandom("Ok", "Pending"));

                if (withId) faker.RuleFor(x => x.Id, f => f.IndexFaker + 1);
                if (randomClient) faker.RuleFor(x => x.CustomerId, f => (uint)f.Random.Int(1, 1000));

                return faker;
            }
        }

        public class Test
        {
            public const string TABLE = "TestTable";

            public const string ID = "id";
            public const string NAME = "name";
            public const string NICK = "nick";
            public const string CREATEDAT = "record_created";
            public const string NUMBER = "number";
            public const string GUIDID = "custom_id";
            public const string STATUS = "custom_status";

            public static Row NewRow(int? id, string name, int number = 0, Guid? guid = null)
            {
                return new Row(
                    new Cell(ID, id),
                    new Cell(NAME, name),
                    new Cell(NUMBER, number),
                    new Cell(GUIDID, (guid ?? Guid.NewGuid()).ToString()),
                    new Cell(STATUS, Status.Unknow)
                );
            }

            public static Faker<TestTable> Faker()
            {
                return new Faker<TestTable>()
                    .RuleFor(x => x.Id, f => f.IndexFaker + 1)
                    .RuleFor(x => x.Id2, f => f.IndexFaker)
                    .RuleFor(x => x.Name, f => f.Name.FullName())
                    .RuleFor(x => x.Nick, f => f.Name.Suffix())
                    .RuleFor(x => x.Number, f => f.Random.Number(0, int.MaxValue))
                    .RuleFor(x => x.CustomId, f => f.Random.Guid())
                    .RuleFor(x => x.CustomStatus, f => f.PickRandom<Status>());
            }
        }
    }
}
