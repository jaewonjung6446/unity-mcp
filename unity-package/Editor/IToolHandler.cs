using Newtonsoft.Json.Linq;

namespace McpUnity
{
    public interface IToolHandler
    {
        string Name { get; }
        JObject Execute(JObject parameters);
    }
}
