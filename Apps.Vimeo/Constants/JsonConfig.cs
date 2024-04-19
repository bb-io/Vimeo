using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Apps.Vimeo.Constants;

public static class JsonConfig
{
    public static JsonSerializerSettings JsonSettings => new()
    {
        ContractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };
}