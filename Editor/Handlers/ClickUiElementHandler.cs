using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace McpUnity.Handlers
{
    public class ClickUiElementHandler : IToolHandler
    {
        public string Name => "click_ui_element";

        public JObject Execute(JObject parameters)
        {
            if (!Application.isPlaying)
                return McpServer.CreateError("click_ui_element requires Play Mode", "invalid_state");

            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var path = GetGameObjectHandler.GetPath(go);

            // Build pointer event data
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return McpServer.CreateError("No EventSystem found in scene", "invalid_state");

            var pointerData = new PointerEventData(eventSystem)
            {
                position = GetCenter(go),
                button = PointerEventData.InputButton.Left
            };

            // Execute pointer event sequence
            ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerClickHandler);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Clicked UI element: {path}"
            };
        }

        private static Vector2 GetCenter(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return Vector2.zero;

            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas == null) return Vector2.zero;

            var rect = FindUiElementsHandler.GetScreenRect(rt, canvas);
            return new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
        }
    }
}
