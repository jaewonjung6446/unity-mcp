using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace McpUnity.Handlers
{
    public class CreateUiElementHandler : IToolHandler
    {
        public string Name => "create_ui_element";

        public JObject Execute(JObject parameters)
        {
            var elementType = parameters["elementType"]?.ToString()?.ToLower();
            if (string.IsNullOrEmpty(elementType))
                return McpServer.CreateError("Missing required parameter: elementType", "validation_error");

            var parentPath = parameters["parentPath"]?.ToString();
            var name = parameters["name"]?.ToString();

            // Find or create Canvas
            Canvas canvas = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentGo = GameObject.Find(parentPath);
                if (parentGo != null) canvas = parentGo.GetComponentInParent<Canvas>();
            }
            if (canvas == null)
            {
#if UNITY_2022_1_OR_NEWER
                canvas = Object.FindFirstObjectByType<Canvas>();
#else
                canvas = Object.FindObjectOfType<Canvas>();
#endif
            }

            bool createdCanvas = false;
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
                createdCanvas = true;

                // Ensure EventSystem exists
#if UNITY_2022_1_OR_NEWER
                if (Object.FindFirstObjectByType<EventSystem>() == null)
#else
                if (Object.FindObjectOfType<EventSystem>() == null)
#endif
                {
                    var esGo = new GameObject("EventSystem");
                    esGo.AddComponent<EventSystem>();
                    esGo.AddComponent<StandaloneInputModule>();
                    Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
                }
            }

            Transform parent = canvas.transform;
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentGo = GameObject.Find(parentPath);
                if (parentGo != null) parent = parentGo.transform;
            }

            GameObject element = null;
            switch (elementType)
            {
                case "text":
                    element = CreateText(name ?? "Text", parent);
                    break;
                case "image":
                    element = CreateImage(name ?? "Image", parent);
                    break;
                case "button":
                    element = CreateButton(name ?? "Button", parent);
                    break;
                case "toggle":
                    element = CreateToggle(name ?? "Toggle", parent);
                    break;
                case "slider":
                    element = CreateSlider(name ?? "Slider", parent);
                    break;
                case "inputfield":
                    element = CreateInputField(name ?? "InputField", parent);
                    break;
                case "dropdown":
                    element = CreateDropdown(name ?? "Dropdown", parent);
                    break;
                case "scrollview":
                    element = CreateScrollView(name ?? "ScrollView", parent);
                    break;
                case "panel":
                    element = CreatePanel(name ?? "Panel", parent);
                    break;
                case "rawimage":
                    element = CreateRawImage(name ?? "RawImage", parent);
                    break;
                default:
                    return McpServer.CreateError($"Unknown element type: {elementType}. Valid: text, image, button, toggle, slider, inputfield, dropdown, scrollview, panel, rawimage", "validation_error");
            }

            Undo.RegisterCreatedObjectUndo(element, $"Create UI {elementType}");

            // Set position
            var rectTransform = element.GetComponent<RectTransform>();
            var anchoredPos = parameters["anchoredPosition"] as JObject;
            if (anchoredPos != null && rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(
                    anchoredPos["x"]?.ToObject<float>() ?? 0f,
                    anchoredPos["y"]?.ToObject<float>() ?? 0f
                );
            }

            // Set size
            var sizeDelta = parameters["sizeDelta"] as JObject;
            if (sizeDelta != null && rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(
                    sizeDelta["x"]?.ToObject<float>() ?? rectTransform.sizeDelta.x,
                    sizeDelta["y"]?.ToObject<float>() ?? rectTransform.sizeDelta.y
                );
            }

            EditorSceneManager.MarkSceneDirty(element.scene);

            var result = new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Created UI {elementType} '{element.name}'",
                ["instanceId"] = element.GetInstanceID(),
                ["path"] = GetGameObjectHandler.GetPath(element)
            };
            if (createdCanvas) result["canvasCreated"] = true;

            return result;
        }

        private GameObject CreateText(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            // Try TMP first via reflection
            var tmpType = GetTMPTextType();
            if (tmpType != null)
            {
                go.AddComponent(tmpType);
            }
            else
            {
                var text = go.AddComponent<Text>();
                text.text = "New Text";
                text.alignment = TextAnchor.MiddleCenter;
            }

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 50);
            return go;
        }

        private GameObject CreateImage(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 100);
            return go;
        }

        private GameObject CreateButton(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>();
            go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 30);

            // Child text
            var textGo = CreateText("Text", go.transform);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            return go;
        }

        private GameObject CreateToggle(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var toggle = go.AddComponent<Toggle>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 20);

            // Background
            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            bg.AddComponent<Image>();
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.sizeDelta = new Vector2(20, 20);
            bgRt.anchoredPosition = new Vector2(10, 0);

            // Checkmark
            var check = new GameObject("Checkmark", typeof(RectTransform));
            check.transform.SetParent(bg.transform, false);
            var checkImg = check.AddComponent<Image>();
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = Vector2.zero;
            checkRt.anchorMax = Vector2.one;
            checkRt.sizeDelta = Vector2.zero;

            toggle.graphic = checkImg;
            toggle.targetGraphic = bg.GetComponent<Image>();

            return go;
        }

        private GameObject CreateSlider(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var slider = go.AddComponent<Slider>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 20);

            // Background
            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            bg.AddComponent<Image>();
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            // Fill Area
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillRt = fillArea.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0.25f);
            fillRt.anchorMax = new Vector2(1, 0.75f);
            fillRt.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            var fillImageRt = fill.GetComponent<RectTransform>();
            fillImageRt.anchorMin = Vector2.zero;
            fillImageRt.anchorMax = Vector2.one;
            fillImageRt.sizeDelta = Vector2.zero;

            slider.fillRect = fillImageRt;
            slider.targetGraphic = bg.GetComponent<Image>();

            return go;
        }

        private GameObject CreateInputField(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 30);

            var textGo = CreateText("Text", go.transform);
            var placeholderGo = CreateText("Placeholder", go.transform);

            // Try TMP InputField via reflection
            var tmpInputType = System.Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            if (tmpInputType != null)
            {
                var inputField = go.AddComponent(tmpInputType);
                // Set references via reflection
                var textProp = tmpInputType.GetProperty("textComponent");
                var placeholderProp = tmpInputType.GetField("m_Placeholder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var tmpText = textGo.GetComponent(GetTMPTextType());
                var tmpPlaceholder = placeholderGo.GetComponent(GetTMPTextType());
                textProp?.SetValue(inputField, tmpText);
                placeholderProp?.SetValue(inputField, tmpPlaceholder as Graphic);
            }
            else
            {
                var inputField = go.AddComponent<InputField>();
                inputField.textComponent = textGo.GetComponent<Text>();
                inputField.placeholder = placeholderGo.GetComponent<Text>();
            }

            return go;
        }

        private GameObject CreateDropdown(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 30);

            // Try TMP Dropdown first
            var tmpDropdownType = System.Type.GetType("TMPro.TMP_Dropdown, Unity.TextMeshPro");
            if (tmpDropdownType != null)
                go.AddComponent(tmpDropdownType);
            else
                go.AddComponent<Dropdown>();

            var label = CreateText("Label", go.transform);
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;

            return go;
        }

        private GameObject CreateScrollView(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>();
            var scrollRect = go.AddComponent<ScrollRect>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 200);

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(go.transform, false);
            viewport.AddComponent<Image>();
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 600);

            scrollRect.viewport = vpRt;
            scrollRect.content = contentRt;

            return go;
        }

        private GameObject CreatePanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.39f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return go;
        }

        private GameObject CreateRawImage(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<RawImage>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 100);
            return go;
        }

        private static System.Type GetTMPTextType()
        {
            return System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        }
    }
}
