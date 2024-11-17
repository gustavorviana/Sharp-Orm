using System.Diagnostics.CodeAnalysis;

namespace BaseTest.Models
{
    public record Address(int Id)
    {
        [AllowNull]
        public string Name { get; set; }

        [AllowNull]
        public string Street { get; set; }
    }
}
