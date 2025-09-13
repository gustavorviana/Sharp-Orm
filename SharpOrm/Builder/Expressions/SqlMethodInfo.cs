using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    public class SqlMethodInfo : SqlMemberInfo
    {
        [DebuggerHidden]
        public new MethodInfo Member => base.Member as MethodInfo;

        public object[] Args { get; }

        public SqlMethodInfo(Type declaringType, MethodInfo method, object[] args) : base(declaringType, method)
        {
            this.Args = args;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(this.Name).Append('(');
            this.WriteParameters(builder);
            return builder.Append(')').ToString();
        }

        private void WriteParameters(StringBuilder builder)
        {
            var @params = this.Member.GetParameters();
            if (@params.Length < 1) return;

            builder.Append(@params[0].ParameterType.Name);

            for (int i = 1; i < @params.Length; i++)
                builder.Append(", ").Append(@params[i].ParameterType.Name);
        }

        public override Type GetMemberType()
        {
            return Member.ReturnType;
        }
    }
}
