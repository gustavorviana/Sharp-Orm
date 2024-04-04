using System;

namespace SharpOrm.Builder
{
    internal class ReadonlyQueryConfig : QueryConfig
    {
        public override string ApplyNomenclature(string name)
        {
            throw new NotImplementedException();
        }

        public override string EscapeString(string value)
        {
            throw new NotImplementedException();
        }

        public override Grammar NewGrammar(Query query)
        {
            throw new NotImplementedException();
        }
    }
}
