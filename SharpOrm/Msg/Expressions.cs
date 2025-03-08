namespace SharpOrm.Msg
{
    internal static partial class Messages
    {
        public static class Expressions
        {
            public const string FunctionDisabled = "It is not possible to use functions in this operation.";
            public const string SubmembersDisabled = "It is not possible to use the subproperties \"x => x.MainProp.SubProp...\".";
            public const string ArgumentNotSupported = "Argument expression type {0} is not supported.";
            public const string MemberNotSupported = "Member expression type {0} is not supported.";
            public const string LoadIncompatible = "It's not possible to load the {0} '{1}' because its type is incompatible.";
            public const string Invalid = "The provided expression is not valid.";

            public const string NewExpressionDisabled = "The expression \"new { }\" is not supported.";
            public const string NativeTypeInTableName = "The type of the table name cannot be a native type.";

            public const string OnlyFieldsAndproerties = "Only properties and fields are supported in this operation.";
        }
    }
}
