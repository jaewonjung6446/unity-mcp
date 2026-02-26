using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.EditorCoroutines.Editor;

namespace McpUnity.Handlers
{
    public class DragUiElementHandler : IToolHandler
    {
        public string Name => "drag_ui_element";

        public JObject Execute(JObject parameters)
        {
            if (!Application.isPlaying)
                return McpServer.CreateError("drag_ui_element requires Play Mode", "invalid_state");

            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var sourcePath = GetGameObjectHandler.GetPath(go);
            var sourceCenter = GetCenter(go);

            // Resolve target
            Vector2 targetPos;
            string targetDesc;
            var targetInstanceId = parameters["targetInstanceId"];
            var targetGoPath = parameters["targetGameObjectPath"]?.ToString();
            var targetPosition = parameters["targetPosition"];

            if (targetInstanceId != null || !string.IsNullOrEmpty(targetGoPath))
            {
                var targetParams = new JObject();
                if (targetInstanceId != null) targetParams["instanceId"] = targetInstanceId;
                if (!string.IsNullOrEmpty(targetGoPath)) targetParams["gameObjectPath"] = targetGoPath;

                var targetGo = GetGameObjectHandler.ResolveGameObject(targetParams, out var targetError);
                if (targetGo == null) return targetError;

                targetPos = GetCenter(targetGo);
                targetDesc = GetGameObjectHandler.GetPath(targetGo);
            }
            else if (targetPosition != null)
            {
                targetPos = new Vector2(
                    targetPosition["x"]?.ToObject<float>() ?? 0f,
                    targetPosition["y"]?.ToObject<float>() ?? 0f
                );
                targetDesc = $"({targetPos.x:F0}, {targetPos.y:F0})";
            }
            else
            {
                return McpServer.CreateError(
                    "Must specify target: targetInstanceId/targetGameObjectPath or targetPosition",
                    "validation_error");
            }

            var duration = parameters["duration"]?.ToObject<float>() ?? 0.3f;
            duration = Mathf.Clamp(duration, 0.05f, 10f);

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return McpServer.CreateError("No EventSystem found in scene", "invalid_state");

            // Start drag coroutine (fire-and-forget)
            EditorCoroutineUtility.StartCoroutineOwnerless(
                DragCoroutine(go, eventSystem, sourceCenter, targetPos, duration));

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Dragging '{sourcePath}' to {targetDesc} over {duration}s"
            };
        }

        private IEnumerator DragCoroutine(GameObject go, EventSystem eventSystem,
            Vector2 from, Vector2 to, float duration)
        {
            var pointerData = new PointerEventData(eventSystem)
            {
                position = from,
                button = PointerEventData.InputButton.Left,
                pointerDrag = go,
                dragging = false
            };

            // Enter + Down
            ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerDownHandler);
            pointerData.pointerPress = go;

            yield return null;

            // BeginDrag
            pointerData.dragging = true;
            ExecuteEvents.Execute(go, pointerData, ExecuteEvents.beginDragHandler);
            yield return null;

            // Drag interpolation
            float elapsed = 0f;
            Vector2 prevPos = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                var pos = Vector2.Lerp(from, to, t);
                pointerData.delta = pos - prevPos;
                pointerData.position = pos;
                prevPos = pos;
                ExecuteEvents.Execute(go, pointerData, ExecuteEvents.dragHandler);
                yield return null;
            }

            // Final position
            pointerData.position = to;

            // Find drop target via raycast
            var raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, raycastResults);
            GameObject dropTarget = null;
            foreach (var hit in raycastResults)
            {
                if (hit.gameObject != go)
                {
                    dropTarget = hit.gameObject;
                    break;
                }
            }

            // EndDrag + Drop + PointerUp
            ExecuteEvents.Execute(go, pointerData, ExecuteEvents.endDragHandler);
            if (dropTarget != null)
                ExecuteEvents.Execute(dropTarget, pointerData, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerUpHandler);
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
