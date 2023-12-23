using System.Text;
using System.Linq;

namespace SharpOrm.Builder
{
    public class MysqlQueryConfig : QueryConfig
    {
        private static string unsafeChars = "\\¥Š₩∖﹨＼\"'`\u00b4ʹʺʻʼˈˊˋ\u02d9\u0300\u0301‘’‚′‵❛❜＇";

        public MysqlQueryConfig()
        {

        }

        public MysqlQueryConfig(bool safeModificationsOnly) : base(safeModificationsOnly)
        {
        }

        public override Grammar NewGrammar(Query query)
        {
            return new MysqlGrammar(query);
        }

        public override string ApplyNomenclature(string name)
        {
            return name.SanitizeSqlName('`', '`');
        }

        public override string EscapeString(string value)
        {
            StringBuilder build = new StringBuilder(value.Length + 2);
            build.Append('"');
            foreach (char c in value)
            {
                if (IsUnsafe(c)) build.Append("\\");

                build.Append(c);
            }
            return build.Append('"').ToString();
        }

        public static bool IsUnsafe(char c)
        {
            return unsafeChars.Contains(c);
        }
    }
}
