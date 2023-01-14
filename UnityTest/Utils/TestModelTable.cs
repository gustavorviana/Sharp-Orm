using SharpOrm;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnityTest.Utils
{
    [Table("TestTable")]
    public class TestModelTable : Model
    {
        public int Id => this.GetValueOrDefault<int>("id");
        public string Name => this.GetStringOrDefault("name");
        public string Nick => this.GetStringOrDefault("nick");
        public DateTime CreatedAt => this.GetValueOrDefault<DateTime>("record_created");
    }
}
