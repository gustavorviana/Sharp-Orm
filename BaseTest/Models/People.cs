using SharpOrm;

namespace BaseTest.Models
{
    public class SystemUser
    {
        [Json]
        public People? People { get; set; }
        public string? Password { get; set; }
    }

    public class People
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }
}
