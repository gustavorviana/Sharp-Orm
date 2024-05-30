namespace UnityTest.Models
{
    public record Address(int Id)
    {
        public string Name { get; set; }
        public string Street { get; set; }
    }
}
