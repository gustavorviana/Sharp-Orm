using System;

namespace SharpOrm
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class JsonAttribute : Attribute
    {
    }
}
