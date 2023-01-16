using SharpOrm;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnityTest.Models
{
    [Table("TestTable")]
    public class TestModelTable : Model
    {
        public int Id => GetValueOrDefault<int>("id");
        public string Name => GetStringOrDefault("name");
        public string Nick => GetStringOrDefault("nick");
        public DateTime CreatedAt => GetValueOrDefault<DateTime>("record_created");
    }
}
