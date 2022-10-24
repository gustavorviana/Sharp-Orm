using SharpOrm;
using System;

namespace UnityTest.Utils
{
    public class TestTable : Model
    {
        public int Id => this.GetValueOrDefault<int>("id");
        public string Name => this.GetStringOrDefault("name");
        public string Nick => this.GetStringOrDefault("nick");
        public DateTime CreatedAt => this.GetValueOrDefault<DateTime>("record_created");
    }
}
