using Newtonsoft.Json;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.Samples.CustomTypes
{
    public class InvalidCustomType
    {
        [JsonProperty("name")]
        public string Name;

        [McpArgument(Description = "年龄")]
        public int Age;

        [JsonProperty("email")]
        [McpArgument(Description = "邮箱")]
        public string Email;
    }
}
