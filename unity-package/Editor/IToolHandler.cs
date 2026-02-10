using System;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace McpUnity
{
    public interface IToolHandler
    {
        string Name { get; }
        JObject Execute(JObject parameters);
    }

    /// <summary>
    /// Extended handler interface for tools that need coroutine-based execution (e.g., polling, timed sequences).
    /// </summary>
    public interface ICoroutineToolHandler : IToolHandler
    {
        IEnumerator ExecuteCoroutine(JObject parameters, Action<JObject> onComplete);
    }
}
