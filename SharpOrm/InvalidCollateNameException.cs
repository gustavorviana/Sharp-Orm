using System;

namespace SharpOrm
{
    public class InvalidCollateNameException : Exception
    {
        public string Name { get; }

        public InvalidCollateNameException(string name) : base("The collate has invalid characters.")
        {
            Name = name;
        }
    }
}
