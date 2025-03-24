namespace SharpOrm.Msg
{
    internal static partial class Messages
    {
        public static class ObjectActivator
        {
            public const string ArrayType = "It's not possible to instantiate an array type.";
            public const string EnumType = "It's not possible to instantiate an enum type.";
            public const string AbstractType = "It's not possible to instantiate an abstract type.";
            public const string InterfaceType = "It's not possible to instantiate an interface type.";
            public const string NoSuitableConstructor = "A compatible constructor for the received data could not be found.";
        }
    }
}
