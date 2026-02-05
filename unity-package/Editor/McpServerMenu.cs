using UnityEditor;
using UnityEngine;

namespace McpUnity
{
    public static class McpServerMenu
    {
        private const string MenuStart = "Tools/MCP Server/Start";
        private const string MenuStop = "Tools/MCP Server/Stop";
        private const string MenuAutoStart = "Tools/MCP Server/Auto Start";

        private const string AutoStartPrefKey = "McpUnity_AutoStart";

        public static bool AutoStart
        {
            get => EditorPrefs.GetBool(AutoStartPrefKey, true);
            set => EditorPrefs.SetBool(AutoStartPrefKey, value);
        }

        [MenuItem(MenuStart, priority = 0)]
        private static void StartServer()
        {
            McpServer.Instance.Start();
        }

        [MenuItem(MenuStart, true)]
        private static bool StartServerValidate()
        {
            return !McpServer.Instance.IsListening;
        }

        [MenuItem(MenuStop, priority = 1)]
        private static void StopServer()
        {
            McpServer.Instance.Stop();
        }

        [MenuItem(MenuStop, true)]
        private static bool StopServerValidate()
        {
            return McpServer.Instance.IsListening;
        }

        [MenuItem(MenuAutoStart, priority = 20)]
        private static void ToggleAutoStart()
        {
            AutoStart = !AutoStart;
        }

        [MenuItem(MenuAutoStart, true)]
        private static bool ToggleAutoStartValidate()
        {
            Menu.SetChecked(MenuAutoStart, AutoStart);
            return true;
        }
    }
}
