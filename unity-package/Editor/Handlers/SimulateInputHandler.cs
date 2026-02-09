using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.EditorCoroutines.Editor;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.LowLevel;
#endif

namespace McpUnity.Handlers
{
    public class SimulateInputHandler : IToolHandler
    {
        public string Name => "simulate_input";

        public JObject Execute(JObject parameters)
        {
            if (!Application.isPlaying)
                return McpServer.CreateError("simulate_input requires Play Mode", "invalid_state");

            var action = parameters["action"]?.ToString();
            if (string.IsNullOrEmpty(action))
                return McpServer.CreateError("Missing required parameter: action", "validation_error");

            switch (action.ToLower())
            {
                case "keydown":
                    return HandleKeyDown(parameters);
                case "keyup":
                    return HandleKeyUp(parameters);
                case "mouseclick":
                    return HandleMouseClick(parameters);
                case "mousemove":
                    return HandleMouseMove(parameters);
                case "hold":
                    return HandleHold(parameters);
                default:
                    return McpServer.CreateError($"Unknown action: {action}. Supported: keyDown, keyUp, mouseClick, mouseMove, hold", "validation_error");
            }
        }

        private JObject HandleKeyDown(JObject parameters)
        {
            var keyStr = parameters["key"]?.ToString();
            if (string.IsNullOrEmpty(keyStr))
                return McpServer.CreateError("Missing required parameter: key", "validation_error");

#if ENABLE_INPUT_SYSTEM
            try
            {
                return SimulateNewInputSystem(keyStr, true);
            }
            catch
            {
                // Fall through to legacy
            }
#endif
            return SimulateLegacyKeyEvent(keyStr, "keyDown");
        }

        private JObject HandleKeyUp(JObject parameters)
        {
            var keyStr = parameters["key"]?.ToString();
            if (string.IsNullOrEmpty(keyStr))
                return McpServer.CreateError("Missing required parameter: key", "validation_error");

#if ENABLE_INPUT_SYSTEM
            try
            {
                return SimulateNewInputSystem(keyStr, false);
            }
            catch
            {
                // Fall through to legacy
            }
#endif
            return SimulateLegacyKeyEvent(keyStr, "keyUp");
        }

        private JObject HandleMouseClick(JObject parameters)
        {
            var posObj = parameters["position"];
            float x = posObj?["x"]?.ToObject<float>() ?? Screen.width * 0.5f;
            float y = posObj?["y"]?.ToObject<float>() ?? Screen.height * 0.5f;

#if ENABLE_INPUT_SYSTEM
            try
            {
                return SimulateNewInputMouseClick(x, y);
            }
            catch
            {
                // Fall through to EventSystem approach
            }
#endif
            return SimulateEventSystemClick(x, y);
        }

        private JObject HandleMouseMove(JObject parameters)
        {
            var posObj = parameters["position"];
            if (posObj == null)
                return McpServer.CreateError("Missing required parameter: position {x, y}", "validation_error");

            float x = posObj["x"]?.ToObject<float>() ?? 0f;
            float y = posObj["y"]?.ToObject<float>() ?? 0f;

#if ENABLE_INPUT_SYSTEM
            try
            {
                return SimulateNewInputMouseMove(x, y);
            }
            catch
            {
                // Fall through
            }
#endif
            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Mouse move to ({x}, {y}) — Legacy Input does not support direct mouse position simulation. Use New Input System for full support."
            };
        }

        private JObject HandleHold(JObject parameters)
        {
            var keyStr = parameters["key"]?.ToString();
            if (string.IsNullOrEmpty(keyStr))
                return McpServer.CreateError("Missing required parameter: key", "validation_error");

            var duration = parameters["duration"]?.ToObject<float>() ?? 0.5f;
            if (duration < 0.05f) duration = 0.05f;
            if (duration > 30f) duration = 30f;

            // Queue key down, wait, then key up via coroutine
#if ENABLE_INPUT_SYSTEM
            try
            {
                SimulateNewInputSystem(keyStr, true);
                EditorCoroutineUtility.StartCoroutineOwnerless(ReleaseKeyAfterDelay(keyStr, duration));
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Holding key '{keyStr}' for {duration}s (New Input System)"
                };
            }
            catch { }
#endif
            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Hold key '{keyStr}' for {duration}s — Legacy Input does not support programmatic key hold. Consider using New Input System or execute_code for custom input handling."
            };
        }

        private IEnumerator ReleaseKeyAfterDelay(string keyStr, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
#if ENABLE_INPUT_SYSTEM
            try { SimulateNewInputSystem(keyStr, false); } catch { }
#endif
        }

        // ---- New Input System ----

#if ENABLE_INPUT_SYSTEM
        private JObject SimulateNewInputSystem(string keyStr, bool pressed)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null)
                return McpServer.CreateError("No keyboard device found in Input System", "invalid_state");

            var key = FindKey(keyboard, keyStr);
            if (key == null)
                return McpServer.CreateError($"Unknown key: {keyStr}", "validation_error");

            using (StateEvent.From(keyboard, out var eventPtr))
            {
                key.WriteValueIntoEvent(pressed ? 1f : 0f, eventPtr);
                UnityEngine.InputSystem.InputSystem.QueueEvent(eventPtr);
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Key {(pressed ? "down" : "up")}: {keyStr} (New Input System)"
            };
        }

        private UnityEngine.InputSystem.Controls.KeyControl FindKey(UnityEngine.InputSystem.Keyboard keyboard, string keyStr)
        {
            // Try exact match first
            try
            {
                var control = keyboard[keyStr.ToLower()];
                if (control is UnityEngine.InputSystem.Controls.KeyControl kc)
                    return kc;
            }
            catch { }

            // Common aliases
            switch (keyStr.ToLower())
            {
                case "w": return keyboard.wKey;
                case "a": return keyboard.aKey;
                case "s": return keyboard.sKey;
                case "d": return keyboard.dKey;
                case "space": return keyboard.spaceKey;
                case "escape": case "esc": return keyboard.escapeKey;
                case "enter": case "return": return keyboard.enterKey;
                case "tab": return keyboard.tabKey;
                case "shift": case "leftshift": return keyboard.leftShiftKey;
                case "ctrl": case "leftctrl": case "control": return keyboard.leftCtrlKey;
                case "alt": case "leftalt": return keyboard.leftAltKey;
                case "backspace": return keyboard.backspaceKey;
                case "delete": return keyboard.deleteKey;
                case "uparrow": case "up": return keyboard.upArrowKey;
                case "downarrow": case "down": return keyboard.downArrowKey;
                case "leftarrow": case "left": return keyboard.leftArrowKey;
                case "rightarrow": case "right": return keyboard.rightArrowKey;
                case "f1": return keyboard.f1Key;
                case "f2": return keyboard.f2Key;
                case "f3": return keyboard.f3Key;
                case "f4": return keyboard.f4Key;
                case "f5": return keyboard.f5Key;
                case "e": return keyboard.eKey;
                case "q": return keyboard.qKey;
                case "r": return keyboard.rKey;
                case "f": return keyboard.fKey;
                case "g": return keyboard.gKey;
                case "1": return keyboard.digit1Key;
                case "2": return keyboard.digit2Key;
                case "3": return keyboard.digit3Key;
                case "4": return keyboard.digit4Key;
                case "5": return keyboard.digit5Key;
            }

            // Try reflection as last resort
            var prop = typeof(UnityEngine.InputSystem.Keyboard).GetProperty(keyStr + "Key",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (prop != null)
                return prop.GetValue(keyboard) as UnityEngine.InputSystem.Controls.KeyControl;

            return null;
        }

        private JObject SimulateNewInputMouseClick(float x, float y)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null)
                return McpServer.CreateError("No mouse device found in Input System", "invalid_state");

            // Move mouse to position
            using (StateEvent.From(mouse, out var movePtr))
            {
                mouse.position.WriteValueIntoEvent(new Vector2(x, y), movePtr);
                UnityEngine.InputSystem.InputSystem.QueueEvent(movePtr);
            }

            // Press
            using (StateEvent.From(mouse, out var downPtr))
            {
                mouse.position.WriteValueIntoEvent(new Vector2(x, y), downPtr);
                mouse.leftButton.WriteValueIntoEvent(1f, downPtr);
                UnityEngine.InputSystem.InputSystem.QueueEvent(downPtr);
            }

            // Release
            using (StateEvent.From(mouse, out var upPtr))
            {
                mouse.position.WriteValueIntoEvent(new Vector2(x, y), upPtr);
                mouse.leftButton.WriteValueIntoEvent(0f, upPtr);
                UnityEngine.InputSystem.InputSystem.QueueEvent(upPtr);
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Mouse click at ({x}, {y}) (New Input System)"
            };
        }

        private JObject SimulateNewInputMouseMove(float x, float y)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null)
                return McpServer.CreateError("No mouse device found in Input System", "invalid_state");

            using (StateEvent.From(mouse, out var eventPtr))
            {
                mouse.position.WriteValueIntoEvent(new Vector2(x, y), eventPtr);
                UnityEngine.InputSystem.InputSystem.QueueEvent(eventPtr);
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Mouse moved to ({x}, {y}) (New Input System)"
            };
        }
#endif

        // ---- Legacy Input / EventSystem fallback ----

        private JObject SimulateLegacyKeyEvent(string keyStr, string eventType)
        {
            // Legacy Input doesn't support programmatic key simulation directly.
            // We can broadcast a message to all MonoBehaviours as a workaround.
            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Key {eventType}: {keyStr} — Legacy Input Manager detected. Direct key simulation is not supported. Use execute_code to call game-specific input methods, or install the New Input System package for full simulation support."
            };
        }

        private JObject SimulateEventSystemClick(float x, float y)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return McpServer.CreateError("No EventSystem found in scene", "invalid_state");

            var pointerData = new PointerEventData(eventSystem)
            {
                position = new Vector2(x, y),
                button = PointerEventData.InputButton.Left
            };

            // Raycast to find what's under the pointer
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, raycastResults);

            if (raycastResults.Count == 0)
            {
                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = $"Mouse click at ({x}, {y}) — no UI element hit by raycast"
                };
            }

            var hitGo = raycastResults[0].gameObject;
            pointerData.pointerCurrentRaycast = raycastResults[0];

            ExecuteEvents.Execute(hitGo, pointerData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(hitGo, pointerData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hitGo, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hitGo, pointerData, ExecuteEvents.pointerClickHandler);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Mouse click at ({x}, {y}) hit '{GetGameObjectHandler.GetPath(hitGo)}' (EventSystem raycast)"
            };
        }
    }
}
