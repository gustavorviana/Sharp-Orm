using System;

namespace SharpOrm.Builder
{
    internal class ReadonlyQueryConfig : QueryConfig
    {
        public override string ApplyNomenclature(string name)
        {
            return name;
        }

        public override string EscapeString(string value)
        {
            throw new NotImplementedException();
        }

        public override Grammar NewGrammar(Query query)
        {
            throw new NotImplementedException();
        }

        public override QueryConfig Clone()
        {
            return new ReadonlyQueryConfig();
        }
    }
}
