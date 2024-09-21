using System.Text;

namespace SharpOrm.Fb
{
    internal class FbSqlExpression : SqlExpression
    {
        private readonly bool isBlock = false;

        public FbSqlExpression(bool isBlock, StringBuilder value, params object[] parameters) : base(value, parameters)
        {
            this.isBlock = isBlock;
        }

        protected internal override string GetScriptParamName(int index)
        {
            if (this.isBlock) return string.Concat(":p", index);
            return base.GetScriptParamName(index);
        }

        protected internal override string GetParamName(int index)
        {
            

            return base.GetParamName(index);
        }
    }
}