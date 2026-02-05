using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class ScreenshotHandler : IToolHandler
    {
        public string Name => "screenshot";

        public JObject Execute(JObject parameters)
        {
            var view = parameters["view"]?.ToString() ?? "scene";
            string tempPath = Path.Combine(Path.GetTempPath(), $"mcp_screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            try
            {
                if (view == "game")
                {
                    // Find the Game view window and read its pixels directly
                    var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                    if (gameViewType == null)
                        return McpServer.CreateError("Could not find GameView type", "screenshot_error");

                    var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                    if (gameView == null)
                        return McpServer.CreateError("No Game View window found", "not_found_error");

                    gameView.Repaint();

                    int width = (int)gameView.position.width;
                    int height = (int)gameView.position.height;
                    var colors = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(
                        gameView.position.position, width, height);

                    var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                    tex.SetPixels(colors);
                    tex.Apply();

                    File.WriteAllBytes(tempPath, tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex);
                }
                else
                {
                    var sceneView = SceneView.lastActiveSceneView;
                    if (sceneView == null)
                        return McpServer.CreateError("No active Scene View", "not_found_error");

                    sceneView.Repaint();

                    int width = (int)sceneView.position.width;
                    int height = (int)sceneView.position.height;
                    var colors = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(
                        sceneView.position.position, width, height);

                    var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                    tex.SetPixels(colors);
                    tex.Apply();

                    File.WriteAllBytes(tempPath, tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex);
                }

                byte[] imageBytes = File.ReadAllBytes(tempPath);
                string base64 = Convert.ToBase64String(imageBytes);

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "image",
                    ["message"] = $"Screenshot captured ({view} view)",
                    ["mimeType"] = "image/png",
                    ["data"] = base64
                };
            }
            catch (Exception ex)
            {
                return McpServer.CreateError($"Screenshot failed: {ex.Message}", "screenshot_error");
            }
            finally
            {
                if (File.Exists(tempPath))
                    try { File.Delete(tempPath); } catch { }
            }
        }
    }
}
