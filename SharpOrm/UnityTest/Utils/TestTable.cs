using SharpOrm;
using System;

namespace UnityTest.Utils
{
    public class TestTable : Model
    {
        public int Id => this.GetValue<int>("id");
        public string Name => this.GetStringValue("name");
        public string Nick => this.GetStringValue("nick");
        public DateTime CreatedAt => this.GetValue<DateTime>("record_created");
    }
}
