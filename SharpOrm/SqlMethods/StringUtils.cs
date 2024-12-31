using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpOrm.SqlMethods
{
    public static class StringUtils
    {
        private struct ReplaceInfo
        {
            public int Index { get; }
            public int Length { get; }
            public string ToReplace { get; }

            public ReplaceInfo(int index, KeyValuePair<string, string> toReplace)
            {
                Index = index;
                Length = toReplace.Key.Length;
                ToReplace = toReplace.Value;
            }

            public void ApplyToBuilder(StringBuilder builder)
            {
                builder.Remove(Index, Length).Insert(Index, ToReplace);
            }
        }

        public static string ReplaceAll(this string value, params KeyValuePair<string, string>[] replaces)
        {
            var builder = new StringBuilder(value);
            var replaceInfos = FindReplaceIndexes(value, replaces);


            for (int i = replaceInfos.Count - 1; i >= 0; i--)
                replaceInfos[i].ApplyToBuilder(builder);

            return builder.ToString();
        }

        private static List<ReplaceInfo> FindReplaceIndexes(string value, IList<KeyValuePair<string, string>> replaces)
        {
            var foundReplaces = new List<ReplaceInfo>();

            for (int i = 0; i < replaces.Count; i++)
            {
                int index = value.IndexOf(replaces[i].Key);
                if (index >= 0 && !foundReplaces.Any(x => x.Index == index))
                    foundReplaces.Add(new ReplaceInfo(index, replaces[i]));
            }

            return foundReplaces;
        }
    }
}
