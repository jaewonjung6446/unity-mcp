using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace McpUnity.Handlers
{
    public class SetRigidbodyPropertyHandler : IToolHandler
    {
        public string Name => "set_rigidbody_property";

        public JObject Execute(JObject parameters)
        {
            var go = GetGameObjectHandler.ResolveGameObject(parameters, out var error);
            if (go == null) return error;

            var rb = go.GetComponent<Rigidbody>();
            var rb2d = go.GetComponent<Rigidbody2D>();

            if (rb == null && rb2d == null)
                return McpServer.CreateError($"No Rigidbody/Rigidbody2D found on '{go.name}'", "not_found_error");

            var changed = new JArray();

            if (rb != null)
            {
                Undo.RecordObject(rb, $"Set Rigidbody on {go.name}");

                var mass = parameters["mass"];
                if (mass != null) { rb.mass = mass.ToObject<float>(); changed.Add("mass"); }

                var drag = parameters["drag"];
                if (drag != null) { rb.linearDamping = drag.ToObject<float>(); changed.Add("drag"); }

                var angularDrag = parameters["angularDrag"];
                if (angularDrag != null) { rb.angularDamping = angularDrag.ToObject<float>(); changed.Add("angularDrag"); }

                var useGravity = parameters["useGravity"];
                if (useGravity != null) { rb.useGravity = useGravity.ToObject<bool>(); changed.Add("useGravity"); }

                var isKinematic = parameters["isKinematic"];
                if (isKinematic != null) { rb.isKinematic = isKinematic.ToObject<bool>(); changed.Add("isKinematic"); }

                var constraints = parameters["constraints"]?.ToString();
                if (!string.IsNullOrEmpty(constraints) && System.Enum.TryParse<RigidbodyConstraints>(constraints, true, out var c))
                { rb.constraints = c; changed.Add("constraints"); }

                var velocity = parameters["velocity"] as JObject;
                if (velocity != null && EditorApplication.isPlaying)
                {
                    rb.linearVelocity = new Vector3(
                        velocity["x"]?.ToObject<float>() ?? 0,
                        velocity["y"]?.ToObject<float>() ?? 0,
                        velocity["z"]?.ToObject<float>() ?? 0
                    );
                    changed.Add("velocity");
                }

                EditorUtility.SetDirty(rb);
            }
            else
            {
                Undo.RecordObject(rb2d, $"Set Rigidbody2D on {go.name}");

                var mass = parameters["mass"];
                if (mass != null) { rb2d.mass = mass.ToObject<float>(); changed.Add("mass"); }

                var drag = parameters["drag"];
                if (drag != null) { rb2d.linearDamping = drag.ToObject<float>(); changed.Add("drag"); }

                var angularDrag = parameters["angularDrag"];
                if (angularDrag != null) { rb2d.angularDamping = angularDrag.ToObject<float>(); changed.Add("angularDrag"); }

                var gravityScale = parameters["gravityScale"];
                if (gravityScale != null) { rb2d.gravityScale = gravityScale.ToObject<float>(); changed.Add("gravityScale"); }

                var isKinematic = parameters["isKinematic"];
                if (isKinematic != null) { rb2d.bodyType = isKinematic.ToObject<bool>() ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic; changed.Add("isKinematic"); }

                var velocity = parameters["velocity"] as JObject;
                if (velocity != null && EditorApplication.isPlaying)
                {
                    rb2d.linearVelocity = new Vector2(
                        velocity["x"]?.ToObject<float>() ?? 0,
                        velocity["y"]?.ToObject<float>() ?? 0
                    );
                    changed.Add("velocity");
                }

                EditorUtility.SetDirty(rb2d);
            }

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Updated {changed.Count} Rigidbody properties on '{go.name}'",
                ["changed"] = changed,
                ["is2D"] = rb2d != null
            };
        }
    }
}
