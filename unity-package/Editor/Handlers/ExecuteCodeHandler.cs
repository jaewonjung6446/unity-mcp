using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

namespace McpUnity.Handlers
{
    public class ExecuteCodeHandler : IToolHandler
    {
        public string Name => "execute_code";

        public JObject Execute(JObject parameters)
        {
            var code = parameters["code"]?.ToString();
            if (string.IsNullOrEmpty(code))
                return McpServer.CreateError("Missing required parameter: code", "validation_error");

            try
            {
                // Wrap in executable class if not already wrapped
                string fullCode;
                if (code.Contains("class "))
                {
                    fullCode = code;
                }
                else
                {
                    fullCode = @"
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

public static class McpCodeRunner
{
    public static object Run()
    {
        " + code + @"
        return null;
    }
}";
                }

                var result = CompileAndRun(fullCode);

                return new JObject
                {
                    ["success"] = true,
                    ["type"] = "text",
                    ["message"] = result ?? "Code executed successfully"
                };
            }
            catch (Exception ex)
            {
                return McpServer.CreateError($"Code execution failed: {ex.Message}", "execution_error");
            }
        }

        private string CompileAndRun(string code)
        {
            // Collect referenced assemblies from Unity's compilation pipeline
            var assemblyPaths = new HashSet<string>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.Location))
                        assemblyPaths.Add(asm.Location);
                }
                catch { }
            }

            // Use Microsoft.CSharp.CSharpCodeProvider (Roslyn-backed in Unity 6)
            var provider = new Microsoft.CSharp.CSharpCodeProvider();
            var compilerParams = new System.CodeDom.Compiler.CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
                TreatWarningsAsErrors = false
            };
            compilerParams.ReferencedAssemblies.AddRange(assemblyPaths.ToArray());

            var results = provider.CompileAssemblyFromSource(compilerParams, code);

            if (results.Errors.HasErrors)
            {
                var errors = string.Join("\n", results.Errors.Cast<System.CodeDom.Compiler.CompilerError>().Select(e => e.ToString()));
                throw new Exception("Compilation errors:\n" + errors);
            }

            // Find and invoke McpCodeRunner.Run() or the first static method
            var compiledAssembly = results.CompiledAssembly;
            var runnerType = compiledAssembly.GetType("McpCodeRunner");
            if (runnerType != null)
            {
                var runMethod = runnerType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
                if (runMethod != null)
                {
                    var result = runMethod.Invoke(null, null);
                    return result?.ToString();
                }
            }

            // Fallback: try to find any static method to call
            foreach (var type in compiledAssembly.GetExportedTypes())
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                if (methods.Length > 0)
                {
                    var result = methods[0].Invoke(null, null);
                    return result?.ToString();
                }
            }

            return "Code compiled but no entry point found.";
        }
    }
}
