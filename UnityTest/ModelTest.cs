using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class ModelTest : MysqlTableTest
    {
        [TestMethod]
        public void LoadFromTableModelClass()
        {
            InsertRows(1);

            using var q = new Query<TestModelTable>(Connection);
            q.Where(ID, 1);

            var model = q.FirstOrDefault();
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("User 1", model.Name);
            Assert.IsNull(model.Nick);
            Assert.IsInstanceOfType(model.CreatedAt, typeof(DateTime));
        }

        [TestMethod]
        public void LoadFromTableMetadata()
        {
            InsertRows(1);

            using var q = new Query<WithMetadataUser>(Connection);
            q.Where(ID, 1);
            q.Select((Column)"Id", (Column)"Name", (Column)"Nick as Metadata_Nick", (Column)"record_created");
            var model = q.FirstOrDefault();
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("User 1", model.Name);
            Assert.IsNull(model.Metadata.Nick);
            Assert.IsInstanceOfType(model.Metadata.CreatedAt, typeof(DateTime));
        }

        [TestMethod]
        public void LoadFromTableClass()
        {
            InsertRows(1);

            using var q = new Query<TestTable>(Connection);
            q.Where(ID, 1);

            var model = q.FirstOrDefault();
            Assert.AreEqual(1, model.Id);
            Assert.AreEqual("User 1", model.Name);
            Assert.IsNull(model.Nick);
            Assert.IsInstanceOfType(model.CreatedAt, typeof(DateTime));
        }

        [TestCleanup]
        [TestInitialize]
        public void CleanupTest()
        {
            using var query = NewQuery();
            query.Delete();
        }

        [Table("TestTable")]
        private class WithMetadataUser
        {
            public int Id { get; set; }

            public string Name { get; set; }

            [Required]
            public Metadata Metadata { get; set; }
        }

        private class Metadata
        {
            [Required]
            public string Nick { get; set; }
            [Column("record_created")]
            public DateTime? CreatedAt { get; set; }
        }
    }
}
