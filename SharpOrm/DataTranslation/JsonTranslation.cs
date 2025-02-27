using System;
using System.Reflection;
#if NET5_0_OR_GREATER || NET462_OR_GREATER || NETSTANDARD2_0_OR_GREATER
using System.Text.Json;
#else
using Newtonsoft.Json;
#endif

namespace SharpOrm.DataTranslation
{
    public class JsonTranslation : JsonTranslationBase
    {
#if NET5_0_OR_GREATER || NET462_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        public JsonSerializerOptions Options { get; set; } = new JsonSerializerOptions();
#elif NET45_OR_GREATER
        public JsonSerializerSettings Settings { get; set; } = new JsonSerializerSettings();
#endif

        protected override object Deserialize(string value, Type type)
        {
#if NET5_0_OR_GREATER || NET462_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            return JsonSerializer.Deserialize(value, type);
#else
            return JsonConvert.DeserializeObject(value, type, Settings);
#endif
        }

        protected override string Serialize(object value)
        {
#if NET5_0_OR_GREATER || NET462_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            return JsonSerializer.Serialize(value, Options);
#else
            return JsonConvert.SerializeObject(value, Settings);
#endif
        }
    }
}
