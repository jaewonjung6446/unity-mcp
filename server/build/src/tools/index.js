import { z } from 'zod';
function textResult(response) {
    return {
        content: [{
                type: 'text',
                text: typeof response === 'string' ? response : JSON.stringify(response, null, 2)
            }]
    };
}
function imageResult(response) {
    if (response.type === 'image' && response.data) {
        return {
            content: [{
                    type: 'image',
                    data: response.data,
                    mimeType: response.mimeType || 'image/png'
                }]
        };
    }
    return textResult(response);
}
const tools = [
    {
        name: 'get_state',
        description: 'Get the current Unity Editor state (play mode, active scene, platform, etc.)',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'get_state', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'get_game_object',
        description: 'Retrieve detailed information about a GameObject by instance ID, name, or hierarchical path',
        schema: {
            idOrName: z.string().describe('Instance ID (integer), name, or path like "Canvas/Panel/Button"')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_game_object', params });
            return textResult(r);
        }
    },
    {
        name: 'get_selection',
        description: 'Get currently selected GameObjects in the Unity Editor',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'get_selection', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'focus_game_object',
        description: 'Focus the Scene View on a specific GameObject',
        schema: {
            idOrName: z.string().describe('Instance ID, name, or path of the GameObject to focus')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'focus_game_object', params });
            return textResult(r);
        }
    },
    {
        name: 'open_scene',
        description: 'Open a Unity scene by its asset path',
        schema: {
            scenePath: z.string().describe('The asset path of the scene (e.g., "Assets/Scenes/Main.unity")'),
            additive: z.boolean().optional().describe('Open additively instead of replacing current scene')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'open_scene', params });
            return textResult(r);
        }
    },
    {
        name: 'close_scene',
        description: 'Close a loaded scene by name',
        schema: {
            sceneName: z.string().describe('Name of the scene to close')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'close_scene', params });
            return textResult(r);
        }
    },
    {
        name: 'save_scene',
        description: 'Save the currently active scene',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'save_scene', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'open_prefab',
        description: 'Open a prefab asset in isolation mode for editing',
        schema: {
            assetPath: z.string().describe('Asset path to the prefab (e.g., "Assets/Prefabs/Player.prefab")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'open_prefab', params });
            return textResult(r);
        }
    },
    {
        name: 'create_script',
        description: 'Create a new C# script file in the Unity project',
        schema: {
            filePath: z.string().describe('Asset path for the script (e.g., "Assets/Scripts/MyScript.cs")'),
            content: z.string().describe('The C# source code content')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'create_script', params });
            return textResult(r);
        }
    },
    {
        name: 'execute_code',
        description: 'Execute C# code in the Unity Editor context',
        schema: {
            code: z.string().describe('C# code to execute')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'execute_code', params });
            return textResult(r);
        }
    },
    {
        name: 'get_asset_contents',
        description: 'Get the contents of a Unity asset (text, script, or serialized data)',
        schema: {
            assetPath: z.string().describe('Asset path (e.g., "Assets/Materials/MyMat.mat")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_asset_contents', params });
            return textResult(r);
        }
    },
    {
        name: 'get_asset_importer',
        description: 'Get the import settings for a Unity asset',
        schema: {
            assetPath: z.string().describe('Asset path')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_asset_importer', params });
            return textResult(r);
        }
    },
    {
        name: 'copy_asset',
        description: 'Copy a Unity asset to a new path',
        schema: {
            sourcePath: z.string().describe('Source asset path'),
            destPath: z.string().describe('Destination asset path')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'copy_asset', params });
            return textResult(r);
        }
    },
    {
        name: 'import_asset',
        description: 'Force reimport a Unity asset',
        schema: {
            assetPath: z.string().describe('Asset path to reimport')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'import_asset', params });
            return textResult(r);
        }
    },
    {
        name: 'search',
        description: 'Search for assets or scene objects in the Unity project',
        schema: {
            query: z.string().describe('Search query string'),
            type: z.enum(['asset', 'scene']).optional().describe('Search type: "asset" (default) or "scene"')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'search', params });
            return textResult(r);
        }
    },
    {
        name: 'screenshot',
        description: 'Capture a screenshot of the Unity Editor Scene or Game view',
        schema: {
            view: z.enum(['scene', 'game']).optional().describe('Which view to capture: "scene" (default) or "game"')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'screenshot', params: params ?? {} });
            return imageResult(r);
        }
    },
    {
        name: 'test_active_scene',
        description: 'Run validation tests on the active scene (missing scripts, missing materials, etc.)',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'test_active_scene', params: {} });
            return textResult(r);
        }
    },
    // --- Shader Graph tools ---
    {
        name: 'create_shader_graph',
        description: 'Create a new Shader Graph asset (URP Lit or Unlit template)',
        schema: {
            assetPath: z.string().describe('Asset path for the shader graph (e.g., "Assets/Shaders/MyShader.shadergraph")'),
            templateType: z.enum(['urp_lit', 'urp_unlit', 'urp_canvas']).optional().describe('Template type: "urp_lit" (default), "urp_unlit", or "urp_canvas" (for UI Canvas shaders)'),
            shaderName: z.string().optional().describe('Display name for the shader (defaults to file name)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'create_shader_graph', params });
            return textResult(r);
        }
    },
    {
        name: 'add_shader_graph_node',
        description: 'Add a node to a Shader Graph (SampleTexture2D, Color, Multiply, Add, Lerp, UV, Time, Float, Split, Combine, Fresnel, Noise, etc.)',
        schema: {
            assetPath: z.string().describe('Asset path of the shader graph'),
            nodeType: z.string().describe('Node type (e.g., "SampleTexture2D", "Color", "Multiply", "Add", "Lerp", "UV", "Time", "Float", "Split", "Combine")'),
            positionX: z.number().optional().describe('X position in the graph (default: -400)'),
            positionY: z.number().optional().describe('Y position in the graph (default: 0)'),
            properties: z.record(z.any()).optional().describe('Additional node properties as key-value pairs')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'add_shader_graph_node', params });
            return textResult(r);
        }
    },
    {
        name: 'connect_shader_graph_nodes',
        description: 'Connect two nodes in a Shader Graph by their slot IDs',
        schema: {
            assetPath: z.string().describe('Asset path of the shader graph'),
            sourceNodeId: z.string().describe('ID of the source (output) node'),
            sourceSlotId: z.number().describe('Slot ID on the source node'),
            targetNodeId: z.string().describe('ID of the target (input) node'),
            targetSlotId: z.number().describe('Slot ID on the target node')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'connect_shader_graph_nodes', params });
            return textResult(r);
        }
    },
    {
        name: 'add_shader_graph_property',
        description: 'Add an exposed property to a Shader Graph (shown in Material inspector). Supported types: Color, Float, Vector2, Vector3, Vector4, Texture2D, Boolean, Integer',
        schema: {
            assetPath: z.string().describe('Asset path of the shader graph'),
            propertyName: z.string().describe('Display name of the property'),
            propertyType: z.enum(['Color', 'Float', 'Vector2', 'Vector3', 'Vector4', 'Texture2D', 'Boolean', 'Integer']).describe('Type of the property'),
            referenceName: z.string().optional().describe('Shader reference name (e.g., "_MainColor"). Auto-generated if omitted'),
            defaultValue: z.any().optional().describe('Default value for the property')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'add_shader_graph_property', params });
            return textResult(r);
        }
    },
    {
        name: 'get_shader_graph_node_types',
        description: 'Get all supported Shader Graph node types with their input/output slot definitions, property types, and template types. Use this before creating or editing shader graphs to know available nodes and their slot IDs for connections.',
        schema: {
            nodeType: z.string().optional().describe('Filter by a specific node type name (e.g., "SampleTexture2D"). Omit to get all types.')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_shader_graph_node_types', params });
            return textResult(r);
        }
    },
    // --- Material tools ---
    {
        name: 'create_material',
        description: 'Create a new Material asset with a specified shader',
        schema: {
            assetPath: z.string().describe('Asset path for the material (e.g., "Assets/Materials/MyMat.mat")'),
            shaderName: z.string().optional().describe('Shader name (e.g., "Universal Render Pipeline/Lit"). Defaults to URP Lit'),
            shaderGraphPath: z.string().optional().describe('Asset path to a Shader Graph to use instead of shaderName')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'create_material', params });
            return textResult(r);
        }
    },
    {
        name: 'set_material_property',
        description: 'Set a property on a Material (color, float, int, vector, or texture)',
        schema: {
            assetPath: z.string().describe('Asset path of the material'),
            propertyName: z.string().describe('Property name (e.g., "_BaseColor", "_Metallic")'),
            propertyType: z.enum(['color', 'float', 'int', 'vector', 'texture']).describe('Type of the property value'),
            value: z.any().describe('Property value. Color: {r,g,b,a}. Vector: {x,y,z,w}. Float/Int: number. Texture: asset path string')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_material_property', params });
            return textResult(r);
        }
    },
    // --- Scene analysis & object material assignment ---
    {
        name: 'analyze_scene',
        description: 'Analyze the active scene and return all renderers, materials, shaders, and lights. Use this to understand scene composition before creating or assigning shaders/materials.',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'analyze_scene', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'set_game_object_material',
        description: 'Assign a material asset to a GameObject\'s Renderer component',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the target GameObject (from analyze_scene or get_game_object)'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the GameObject (e.g., "Environment/Cube")'),
            materialPath: z.string().describe('Asset path of the material to apply (e.g., "Assets/Materials/MyMat.mat")'),
            materialIndex: z.number().optional().describe('Material slot index for multi-material renderers (default: 0)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_game_object_material', params });
            return textResult(r);
        }
    },
    // --- Volume / Post-processing tools ---
    {
        name: 'get_volume_settings',
        description: 'Get URP Volume post-processing settings (Bloom, Vignette, ColorAdjustments, etc.) from the scene. Returns all Volume components with their override properties and values.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of a specific Volume GameObject. Omit to scan all Volumes in the scene.'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the Volume GameObject (e.g., "Global Volume")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_volume_settings', params: params ?? {} });
            return textResult(r);
        }
    },
    {
        name: 'set_volume_component',
        description: 'Set properties on a URP Volume post-processing component (Bloom, Vignette, ColorAdjustments, Tonemapping, DepthOfField, MotionBlur, ChromaticAberration, LensDistortion, FilmGrain, WhiteBalance, etc.). Automatically enables overrideState for changed properties.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the Volume GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the Volume GameObject'),
            componentType: z.string().describe('Volume component type name (e.g., "Bloom", "Vignette", "ColorAdjustments", "Tonemapping", "DepthOfField", "MotionBlur", "ChromaticAberration", "LensDistortion", "FilmGrain", "WhiteBalance")'),
            properties: z.record(z.any()).describe('Properties to set as key-value pairs (e.g., { "intensity": 2.0, "threshold": 0.8 }). Color values use {r,g,b,a}, Vector values use {x,y,z,w}.'),
            addIfMissing: z.boolean().optional().describe('Add the component to the profile if missing (default: true)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_volume_component', params });
            return textResult(r);
        }
    },
    // --- UI QA tools ---
    {
        name: 'find_ui_elements',
        description: 'Scan all Canvas UI elements in the scene. Returns path, instanceId, screen rect, component types, interactable state, text content, and depth for each element. Works in both Edit and Play mode.',
        schema: {
            filter: z.string().optional().describe('Filter by component type name (e.g., "Button", "InputField", "Toggle")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'find_ui_elements', params: params ?? {} });
            return textResult(r);
        }
    },
    {
        name: 'inspect_ui_layout',
        description: 'Static analysis of UI layout issues: overlap between selectables, off-screen elements, touch targets too small (<88px), and text overflow. Works in both Edit and Play mode.',
        schema: {
            screenWidth: z.number().optional().describe('Screen width in pixels (default: 1920)'),
            screenHeight: z.number().optional().describe('Screen height in pixels (default: 1080)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'inspect_ui_layout', params: params ?? {} });
            return textResult(r);
        }
    },
    {
        name: 'click_ui_element',
        description: 'Simulate a UI click on a GameObject via ExecuteEvents (pointerEnter → pointerDown → pointerUp → pointerClick). Play Mode only.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the target UI element'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the UI element (e.g., "Canvas/Panel/Button")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'click_ui_element', params });
            return textResult(r);
        }
    },
    {
        name: 'set_ui_input',
        description: 'Set text on an InputField or TMP_InputField and trigger onValueChanged/onEndEdit events. Play Mode only.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the InputField GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the InputField GameObject'),
            text: z.string().describe('Text to set on the input field')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_ui_input', params });
            return textResult(r);
        }
    },
    {
        name: 'get_ui_state',
        description: 'Get the current state of a UI element: Button (interactable), Toggle (isOn), Slider (value/min/max), InputField (text), Dropdown (value/options), plus RectTransform screen rect and text content. Works in both Edit and Play mode.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the UI element'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the UI element')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_ui_state', params });
            return textResult(r);
        }
    },
    {
        name: 'set_play_mode',
        description: 'Enter or exit Unity Play Mode. The transition is async — use get_state to confirm completion.',
        schema: {
            play: z.boolean().describe('true to enter Play Mode, false to exit')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_play_mode', params });
            return textResult(r);
        }
    },
    // --- QA Simulation tools ---
    {
        name: 'get_console_logs',
        description: 'Retrieve Unity console log entries (errors, warnings, exceptions). Uses Application.logMessageReceived buffer. Essential for detecting runtime errors during QA testing.',
        schema: {
            filter: z.string().optional().describe('Filter by log type: "error", "warning", "log", "exception", "assert", or "all" (default: all)'),
            since: z.number().optional().describe('Only return logs after this Unix timestamp in milliseconds'),
            clear: z.boolean().optional().describe('Clear the log buffer after reading (default: false)'),
            maxCount: z.number().optional().describe('Maximum number of log entries to return (default: 100, max: 500)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_console_logs', params: params ?? {} });
            return textResult(r);
        }
    },
    {
        name: 'get_scene_hierarchy',
        description: 'Get a snapshot of the scene GameObject hierarchy tree. Useful for detecting object creation/destruction/activation changes during QA testing.',
        schema: {
            rootPath: z.string().optional().describe('Hierarchical path to start from (e.g., "Canvas/Panel"). Omit for full scene.'),
            maxDepth: z.number().optional().describe('Maximum depth to traverse (default: 10, max: 50)'),
            includeInactive: z.boolean().optional().describe('Include inactive GameObjects (default: true)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_scene_hierarchy', params: params ?? {} });
            return textResult(r);
        }
    },
    {
        name: 'wait_for_seconds',
        description: 'Wait for a specified duration. Useful for waiting for animations, scene transitions, or loading to complete during QA testing. Implemented server-side (no Unity call).',
        schema: {
            seconds: z.number().describe('Duration to wait in seconds (0.1 to 30)')
        },
        handler: async (_bridge, params) => {
            const seconds = Math.max(0.1, Math.min(30, params.seconds));
            await new Promise(resolve => setTimeout(resolve, seconds * 1000));
            return textResult({ success: true, message: `Waited ${seconds}s` });
        }
    },
    {
        name: 'simulate_input',
        description: 'Simulate keyboard/mouse input in Play Mode. Supports New Input System (full key/mouse simulation) and Legacy Input (EventSystem-based mouse clicks). For gameplay testing: WASD movement, Space jump, mouse aiming/clicking.',
        schema: {
            action: z.enum(['keyDown', 'keyUp', 'mouseClick', 'mouseMove', 'hold']).describe('Input action type'),
            key: z.string().optional().describe('Key name for keyboard actions (e.g., "w", "space", "escape", "leftShift")'),
            position: z.object({ x: z.number(), y: z.number() }).optional().describe('Screen position for mouse actions {x, y}'),
            duration: z.number().optional().describe('Hold duration in seconds for "hold" action (default: 0.5, max: 30)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'simulate_input', params });
            return textResult(r);
        }
    },
    {
        name: 'get_component_data',
        description: 'Read serialized field values from any component on a GameObject. Use to verify game state: HP, score, inventory, transform values, etc.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the target GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the GameObject (e.g., "Player")'),
            componentType: z.string().describe('Component type name (e.g., "PlayerHealth", "Rigidbody", "Animator")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_component_data', params });
            return textResult(r);
        }
    },
    {
        name: 'find_objects_by_criteria',
        description: 'Search for GameObjects in the scene by tag, layer, component type, or name. Works with both UI and 3D objects.',
        schema: {
            tag: z.string().optional().describe('Filter by tag (e.g., "Player", "Enemy")'),
            layer: z.string().optional().describe('Filter by layer name or index (e.g., "UI", "Default", "5")'),
            componentType: z.string().optional().describe('Filter by component type name (e.g., "Rigidbody", "AudioSource", "PlayerController")'),
            nameContains: z.string().optional().describe('Filter by name substring (case-insensitive)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'find_objects_by_criteria', params: params ?? {} });
            return textResult(r);
        }
    },
    // --- Extended QA tools ---
    {
        name: 'drag_ui_element',
        description: 'Simulate a UI drag operation via PointerEventData event chain (pointerDown → beginDrag → drag → endDrag → drop). Drag from a source UI element to a target element or screen position. Returns immediately while drag animates over the specified duration. Play Mode only.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the source UI element to drag'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the source UI element'),
            targetInstanceId: z.number().optional().describe('Instance ID of the target UI element to drop on'),
            targetGameObjectPath: z.string().optional().describe('Hierarchical path of the target UI element'),
            targetPosition: z.object({ x: z.number(), y: z.number() }).optional().describe('Target screen position {x, y} (used if no target element specified)'),
            duration: z.number().optional().describe('Drag duration in seconds (default: 0.3, max: 10)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'drag_ui_element', params });
            return textResult(r);
        }
    },
    {
        name: 'scroll_ui',
        description: 'Simulate a scroll event on a UI element (ScrollRect or custom scroll handler). Returns the normalized scroll position if a ScrollRect is found. Play Mode only.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the scroll target'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the scroll target'),
            delta: z.object({ x: z.number(), y: z.number() }).optional().describe('Scroll delta {x, y} (e.g., {x:0, y:-300} to scroll down)'),
            scrollDelta: z.number().optional().describe('Shorthand for vertical scroll delta (positive=up, negative=down)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'scroll_ui', params });
            return textResult(r);
        }
    },
    {
        name: 'set_ui_value',
        description: 'Set the value of a UI component: Slider (float), Toggle (bool), Dropdown/TMP_Dropdown (int index), Scrollbar (float). Automatically triggers onValueChanged events.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the UI element'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the UI element'),
            value: z.any().describe('Value to set (float for Slider/Scrollbar, bool for Toggle, int for Dropdown)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_ui_value', params });
            return textResult(r);
        }
    },
    {
        name: 'wait_until',
        description: 'Wait until a condition is met, with configurable polling interval and timeout. Supported conditions: gameObjectActive, gameObjectInactive, objectExists, objectDestroyed, uiTextEquals, uiTextContains, componentValue. Returns elapsed time on success, or error on timeout.',
        schema: {
            condition: z.enum(['gameObjectActive', 'gameObjectInactive', 'objectExists', 'objectDestroyed', 'uiTextEquals', 'uiTextContains', 'componentValue']).describe('Condition type to wait for'),
            gameObjectPath: z.string().optional().describe('Target GameObject path'),
            instanceId: z.number().optional().describe('Target GameObject instance ID'),
            componentType: z.string().optional().describe('Component type name (for componentValue condition)'),
            fieldName: z.string().optional().describe('Field or property name (for componentValue condition)'),
            expectedValue: z.string().optional().describe('Expected value as string (for text/component conditions)'),
            timeout: z.number().optional().describe('Max wait time in seconds (default: 10, max: 30)'),
            pollInterval: z.number().optional().describe('Polling interval in seconds (default: 0.2)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'wait_until', params });
            return textResult(r);
        }
    },
    {
        name: 'get_animator_state',
        description: 'Get the current Animator state, transition info, and all parameter values for a GameObject with an Animator component. Returns state hash, normalizedTime, length, loop, transition info, and all parameters with their types and values.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the GameObject with Animator'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the GameObject with Animator'),
            layerIndex: z.number().optional().describe('Animator layer index (default: 0)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_animator_state', params });
            return textResult(r);
        }
    },
    {
        name: 'set_animator_parameter',
        description: 'Set an Animator parameter value (Float, Int, Bool) or fire a Trigger on a GameObject with an Animator component. Auto-detects parameter type if not specified.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the GameObject with Animator'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the GameObject with Animator'),
            parameterName: z.string().describe('Name of the Animator parameter'),
            value: z.any().optional().describe('Value to set (float, int, or bool). Not needed for Trigger type'),
            type: z.enum(['float', 'int', 'bool', 'trigger']).optional().describe('Parameter type. Auto-detected from Animator if omitted')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_animator_parameter', params });
            return textResult(r);
        }
    },
    // --- GameObject CRUD ---
    {
        name: 'create_game_object',
        description: 'Create a new GameObject in the scene (empty, primitive, or with specified parent/position/rotation/scale)',
        schema: {
            name: z.string().optional().describe('Name for the new GameObject (default: "GameObject")'),
            primitiveType: z.enum(['Sphere', 'Capsule', 'Cylinder', 'Cube', 'Plane', 'Quad']).optional().describe('Create a primitive mesh object'),
            parentPath: z.string().optional().describe('Hierarchical path of the parent GameObject'),
            position: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe('Local position {x, y, z}'),
            rotation: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe('Local euler angles {x, y, z}'),
            scale: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe('Local scale {x, y, z}')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'create_game_object', params: params ?? {} });
            return textResult(r);
        }
    },
    {
        name: 'delete_game_object',
        description: 'Delete a GameObject from the scene (with Undo support)',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the GameObject')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'delete_game_object', params });
            return textResult(r);
        }
    },
    {
        name: 'set_game_object_property',
        description: 'Set properties on a GameObject: name, tag, layer, active, isStatic, position, rotation, scale. All properties are optional — only specified ones are changed.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the target GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the target GameObject'),
            name: z.string().optional().describe('New name'),
            tag: z.string().optional().describe('New tag'),
            layer: z.string().optional().describe('Layer name or index'),
            active: z.boolean().optional().describe('Set active state'),
            isStatic: z.boolean().optional().describe('Set static flag'),
            position: z.object({ x: z.number().optional(), y: z.number().optional(), z: z.number().optional() }).optional().describe('Local position'),
            rotation: z.object({ x: z.number().optional(), y: z.number().optional(), z: z.number().optional() }).optional().describe('Local euler angles'),
            scale: z.object({ x: z.number().optional(), y: z.number().optional(), z: z.number().optional() }).optional().describe('Local scale')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_game_object_property', params });
            return textResult(r);
        }
    },
    {
        name: 'duplicate_game_object',
        description: 'Duplicate a GameObject one or more times, optionally with position offset between copies',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the GameObject to duplicate'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the GameObject'),
            count: z.number().optional().describe('Number of duplicates (default: 1, max: 100)'),
            offset: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe('Position offset between each duplicate')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'duplicate_game_object', params });
            return textResult(r);
        }
    },
    {
        name: 'group_game_objects',
        description: 'Group multiple GameObjects under a new empty parent. Centers the group at the average position.',
        schema: {
            instanceIds: z.array(z.number()).optional().describe('Array of instance IDs to group'),
            gameObjectPaths: z.array(z.string()).optional().describe('Array of hierarchical paths to group'),
            groupName: z.string().optional().describe('Name for the group parent (default: "Group")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'group_game_objects', params });
            return textResult(r);
        }
    },
    {
        name: 'align_game_object',
        description: 'Align multiple GameObjects along an axis (x, y, or z) to min, max, center, or first object position',
        schema: {
            instanceIds: z.array(z.number()).optional().describe('Array of instance IDs to align. Uses current selection if omitted.'),
            axis: z.enum(['x', 'y', 'z']).describe('Axis to align along'),
            alignTo: z.enum(['min', 'max', 'center', 'first']).optional().describe('Align strategy (default: "center")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'align_game_object', params });
            return textResult(r);
        }
    },
    // --- Component management ---
    {
        name: 'add_component',
        description: 'Add a component to a GameObject (e.g., "Rigidbody", "AudioSource", "BoxCollider", custom types)',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the target GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the GameObject'),
            componentType: z.string().describe('Component type name (e.g., "Rigidbody", "BoxCollider", "AudioSource")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'add_component', params });
            return textResult(r);
        }
    },
    {
        name: 'remove_component',
        description: 'Remove a component from a GameObject (cannot remove Transform)',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the target GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the GameObject'),
            componentType: z.string().describe('Component type name to remove')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'remove_component', params });
            return textResult(r);
        }
    },
    {
        name: 'set_component_property',
        description: 'Set field/property values on a component. Supports int, float, bool, string, Color, Vector2/3/4, enum, and object references (by asset path).',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the target GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the GameObject'),
            componentType: z.string().describe('Component type name (e.g., "BoxCollider", "PlayerHealth")'),
            properties: z.record(z.any()).describe('Key-value pairs of properties to set (e.g., {"isTrigger": true, "size": {"x":2,"y":2,"z":2}})')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_component_property', params });
            return textResult(r);
        }
    },
    // --- Prefab tools ---
    {
        name: 'instantiate_prefab',
        description: 'Instantiate a prefab into the scene with optional name, parent, position, and rotation',
        schema: {
            assetPath: z.string().describe('Asset path of the prefab (e.g., "Assets/Prefabs/Enemy.prefab")'),
            name: z.string().optional().describe('Name override for the instance'),
            parentPath: z.string().optional().describe('Hierarchical path of the parent'),
            position: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe('Local position'),
            rotation: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe('Local euler angles')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'instantiate_prefab', params });
            return textResult(r);
        }
    },
    {
        name: 'create_prefab',
        description: 'Create a prefab asset from an existing scene GameObject',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the source GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the source GameObject'),
            savePath: z.string().describe('Asset path to save the prefab (e.g., "Assets/Prefabs/MyPrefab.prefab")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'create_prefab', params });
            return textResult(r);
        }
    },
    // --- Hierarchy ---
    {
        name: 'set_parent',
        description: 'Set or change the parent of a GameObject. Pass null/empty parent to unparent to scene root.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the child GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the child GameObject'),
            parentPath: z.string().optional().describe('Hierarchical path of the new parent (omit to unparent)'),
            parentInstanceId: z.number().optional().describe('Instance ID of the new parent'),
            worldPositionStays: z.boolean().optional().describe('Keep world position (default: true)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_parent', params });
            return textResult(r);
        }
    },
    // --- Asset management extended ---
    {
        name: 'delete_asset',
        description: 'Delete an asset or folder from the Unity project',
        schema: {
            assetPath: z.string().describe('Asset path to delete (e.g., "Assets/Scripts/OldScript.cs")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'delete_asset', params });
            return textResult(r);
        }
    },
    {
        name: 'move_asset',
        description: 'Move or rename a Unity asset',
        schema: {
            sourcePath: z.string().describe('Current asset path'),
            destPath: z.string().describe('New asset path')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'move_asset', params });
            return textResult(r);
        }
    },
    {
        name: 'create_folder',
        description: 'Create a folder in the Unity project (creates parent folders recursively)',
        schema: {
            folderPath: z.string().describe('Folder path to create (e.g., "Assets/Materials/Characters")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'create_folder', params });
            return textResult(r);
        }
    },
    {
        name: 'get_asset_dependencies',
        description: 'Get all assets that an asset depends on (textures, materials, scripts, etc.)',
        schema: {
            assetPath: z.string().describe('Asset path to check dependencies for'),
            recursive: z.boolean().optional().describe('Include transitive dependencies (default: true)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_asset_dependencies', params });
            return textResult(r);
        }
    },
    {
        name: 'find_references',
        description: 'Find all assets that reference/use a given asset (reverse dependency search)',
        schema: {
            assetPath: z.string().describe('Asset path to find references for'),
            filter: z.string().optional().describe('Limit search to assets under this path prefix')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'find_references', params });
            return textResult(r);
        }
    },
    {
        name: 'get_asset_preview',
        description: 'Get a preview thumbnail image of an asset (prefab, material, texture, model, etc.)',
        schema: {
            assetPath: z.string().describe('Asset path to preview')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_asset_preview', params });
            return imageResult(r);
        }
    },
    {
        name: 'refresh_assets',
        description: 'Force Unity to reimport/refresh the AssetDatabase (equivalent to Ctrl+R)',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'refresh_assets', params: {} });
            return textResult(r);
        }
    },
    // --- Editor operations ---
    {
        name: 'undo_redo',
        description: 'Perform undo or redo operations in the Unity Editor',
        schema: {
            action: z.enum(['undo', 'redo']).describe('Undo or redo'),
            steps: z.number().optional().describe('Number of steps (default: 1, max: 20)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'undo_redo', params });
            return textResult(r);
        }
    },
    {
        name: 'set_selection',
        description: 'Set the Editor selection to a GameObject, asset, or multiple objects. Pass no parameters to clear selection.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of a GameObject to select'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of a GameObject to select'),
            instanceIds: z.array(z.number()).optional().describe('Array of instance IDs for multi-selection'),
            assetPath: z.string().optional().describe('Asset path to select in Project window')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_selection', params: params ?? {} });
            return textResult(r);
        }
    },
    {
        name: 'pause_editor',
        description: 'Pause or unpause the Unity Editor (during Play Mode)',
        schema: {
            pause: z.boolean().optional().describe('true to pause, false to unpause. Toggles if omitted.')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'pause_editor', params: params ?? {} });
            return textResult(r);
        }
    },
    {
        name: 'step_frame',
        description: 'Advance one frame while paused in Play Mode (frame-by-frame debugging)',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'step_frame', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'clear_console',
        description: 'Clear the Unity Console window and the internal log buffer',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'clear_console', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'set_scene_view_camera',
        description: 'Control the Scene View camera: position, rotation, zoom, orthographic mode, or preset views (top, front, right, etc.)',
        schema: {
            position: z.object({ x: z.number().optional(), y: z.number().optional(), z: z.number().optional() }).optional().describe('Camera pivot position'),
            rotation: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe('Camera euler rotation'),
            size: z.number().optional().describe('Zoom level (orthographic size or distance)'),
            orthographic: z.boolean().optional().describe('Switch to orthographic (true) or perspective (false)'),
            preset: z.enum(['top', 'bottom', 'front', 'back', 'left', 'right', 'perspective']).optional().describe('Quick preset view')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_scene_view_camera', params: params ?? {} });
            return textResult(r);
        }
    },
    // --- Project info ---
    {
        name: 'get_scenes_list',
        description: 'Get all scenes in the project: build settings scenes, currently loaded scenes, and all scene files',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'get_scenes_list', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'get_project_settings',
        description: 'Get Unity project settings (player, quality, physics, time, tags/layers)',
        schema: {
            category: z.enum(['all', 'player', 'quality', 'physics', 'time', 'tags_and_layers']).optional().describe('Settings category (default: "all")')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_project_settings', params: params ?? {} });
            return textResult(r);
        }
    },
    {
        name: 'get_packages',
        description: 'List all installed Unity packages from manifest.json with version info',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'get_packages', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'get_build_settings',
        description: 'Get build settings: target platform, scenes in build, development mode, etc.',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'get_build_settings', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'get_missing_references',
        description: 'Scan for missing script references and broken object references in scene objects or project assets',
        schema: {
            scope: z.enum(['scene', 'assets', 'all']).optional().describe('Where to search: "scene" (default), "assets", or "all"')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_missing_references', params: params ?? {} });
            return textResult(r);
        }
    },
    // --- Rendering ---
    {
        name: 'get_render_settings',
        description: 'Get scene render settings: ambient lighting, fog, skybox, reflections, render pipeline info',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'get_render_settings', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'set_render_settings',
        description: 'Set scene render settings: fog, ambient lighting, skybox, reflections, halo/flare. Only specified properties are changed.',
        schema: {
            fog: z.boolean().optional().describe('Enable/disable fog'),
            fogColor: z.object({ r: z.number(), g: z.number(), b: z.number(), a: z.number().optional() }).optional().describe('Fog color'),
            fogMode: z.enum(['Linear', 'Exponential', 'ExponentialSquared']).optional().describe('Fog mode'),
            fogDensity: z.number().optional().describe('Fog density (for exponential modes)'),
            fogStartDistance: z.number().optional().describe('Fog start distance (for linear mode)'),
            fogEndDistance: z.number().optional().describe('Fog end distance (for linear mode)'),
            ambientMode: z.enum(['Skybox', 'Trilight', 'Flat', 'Custom']).optional().describe('Ambient lighting mode'),
            ambientSkyColor: z.object({ r: z.number(), g: z.number(), b: z.number(), a: z.number().optional() }).optional().describe('Ambient sky color'),
            ambientEquatorColor: z.object({ r: z.number(), g: z.number(), b: z.number(), a: z.number().optional() }).optional().describe('Ambient equator color (for Trilight)'),
            ambientGroundColor: z.object({ r: z.number(), g: z.number(), b: z.number(), a: z.number().optional() }).optional().describe('Ambient ground color (for Trilight)'),
            ambientIntensity: z.number().optional().describe('Ambient intensity multiplier'),
            reflectionIntensity: z.number().optional().describe('Reflection intensity'),
            reflectionBounces: z.number().optional().describe('Reflection bounces'),
            haloStrength: z.number().optional().describe('Halo strength'),
            flareStrength: z.number().optional().describe('Flare strength'),
            skyboxMaterialPath: z.string().optional().describe('Asset path to skybox material')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_render_settings', params });
            return textResult(r);
        }
    },
    {
        name: 'set_light_property',
        description: 'Set properties on a Light component: type, color, intensity, range, shadows, spot angle, temperature',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the Light GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the Light GameObject'),
            lightType: z.enum(['Directional', 'Point', 'Spot', 'Area']).optional().describe('Light type'),
            color: z.object({ r: z.number(), g: z.number(), b: z.number(), a: z.number().optional() }).optional().describe('Light color'),
            intensity: z.number().optional().describe('Light intensity'),
            range: z.number().optional().describe('Light range (Point/Spot)'),
            spotAngle: z.number().optional().describe('Spot angle (Spot only)'),
            shadows: z.enum(['None', 'Hard', 'Soft']).optional().describe('Shadow type'),
            shadowStrength: z.number().optional().describe('Shadow strength (0-1)'),
            colorTemperature: z.number().optional().describe('Color temperature in Kelvin'),
            enabled: z.boolean().optional().describe('Enable/disable the light')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_light_property', params });
            return textResult(r);
        }
    },
    // --- Physics ---
    {
        name: 'raycast',
        description: 'Perform a physics raycast from a world-space origin+direction or from a screen point through the main camera',
        schema: {
            origin: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe('Ray origin in world space'),
            direction: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe('Ray direction in world space'),
            screenPoint: z.object({ x: z.number(), y: z.number() }).optional().describe('Screen point (alternative to origin+direction)'),
            maxDistance: z.number().optional().describe('Maximum ray distance (default: 1000)'),
            layerMask: z.number().optional().describe('Layer mask bitmask (default: all layers)'),
            all: z.boolean().optional().describe('Return all hits instead of first (default: false)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'raycast', params });
            return textResult(r);
        }
    },
    {
        name: 'set_rigidbody_property',
        description: 'Set properties on a Rigidbody or Rigidbody2D: mass, drag, gravity, kinematic, velocity, constraints',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the GameObject'),
            mass: z.number().optional().describe('Mass'),
            drag: z.number().optional().describe('Linear drag'),
            angularDrag: z.number().optional().describe('Angular drag'),
            useGravity: z.boolean().optional().describe('Use gravity (3D only)'),
            gravityScale: z.number().optional().describe('Gravity scale (2D only)'),
            isKinematic: z.boolean().optional().describe('Is kinematic'),
            constraints: z.string().optional().describe('Constraints flags (e.g., "FreezePositionX, FreezeRotationY")'),
            velocity: z.object({ x: z.number(), y: z.number(), z: z.number().optional() }).optional().describe('Set velocity (Play Mode only)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_rigidbody_property', params });
            return textResult(r);
        }
    },
    {
        name: 'apply_force',
        description: 'Apply force to a Rigidbody/Rigidbody2D (Play Mode only). Supports Force, Impulse, VelocityChange, Acceleration modes.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the target GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the target GameObject'),
            force: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe('Force vector {x, y, z}'),
            mode: z.enum(['Force', 'Impulse', 'VelocityChange', 'Acceleration']).optional().describe('Force mode (default: Force)'),
            atPosition: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe('Apply force at world position (3D only)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'apply_force', params });
            return textResult(r);
        }
    },
    {
        name: 'set_time_scale',
        description: 'Set Time.timeScale for slow-motion or fast-forward during Play Mode testing',
        schema: {
            timeScale: z.number().describe('Time scale value (0=pause, 1=normal, 0.5=half speed, 2=double speed, max: 100)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_time_scale', params });
            return textResult(r);
        }
    },
    {
        name: 'get_performance_stats',
        description: 'Get runtime performance statistics: FPS, memory usage, object counts, rendering info',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'get_performance_stats', params: {} });
            return textResult(r);
        }
    },
    // --- Audio ---
    {
        name: 'get_audio_sources',
        description: 'Get all AudioSources in the scene with clip, volume, pitch, loop, spatial blend, and play state info',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'get_audio_sources', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'play_audio',
        description: 'Control audio playback: play, stop, pause, unpause an AudioSource. Optionally set a clip by asset path.',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the AudioSource GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the AudioSource GameObject'),
            action: z.enum(['play', 'stop', 'pause', 'unpause']).optional().describe('Playback action (default: "play")'),
            clipPath: z.string().optional().describe('Asset path to an AudioClip to assign before playing')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'play_audio', params: params ?? {} });
            return textResult(r);
        }
    },
    // --- UI creation ---
    {
        name: 'create_ui_element',
        description: 'Create a UI element (Text, Image, Button, Toggle, Slider, InputField, Dropdown, ScrollView, Panel, RawImage). Auto-creates Canvas and EventSystem if needed.',
        schema: {
            elementType: z.enum(['text', 'image', 'button', 'toggle', 'slider', 'inputfield', 'dropdown', 'scrollview', 'panel', 'rawimage']).describe('Type of UI element to create'),
            name: z.string().optional().describe('Name for the UI element'),
            parentPath: z.string().optional().describe('Parent path (defaults to Canvas)'),
            anchoredPosition: z.object({ x: z.number(), y: z.number() }).optional().describe('Anchored position on the RectTransform'),
            sizeDelta: z.object({ x: z.number(), y: z.number() }).optional().describe('Size delta (width, height)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'create_ui_element', params });
            return textResult(r);
        }
    },
    // --- Navigation ---
    {
        name: 'get_navmesh_info',
        description: 'Get NavMesh information: mesh stats, all NavMeshAgents with paths/status, and NavMeshObstacles',
        schema: {},
        handler: async (bridge) => {
            const r = await bridge.sendRequest({ method: 'get_navmesh_info', params: {} });
            return textResult(r);
        }
    },
    {
        name: 'set_navmesh_destination',
        description: 'Set a NavMeshAgent destination or stop it (Play Mode only)',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the NavMeshAgent GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the NavMeshAgent GameObject'),
            destination: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe('Target world position'),
            stop: z.boolean().optional().describe('Stop the agent and clear path (default: false)')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'set_navmesh_destination', params });
            return textResult(r);
        }
    },
    // --- Scene systems ---
    {
        name: 'get_particle_system_info',
        description: 'Get ParticleSystem info: main module settings, emission, shape, play state, particle count',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the ParticleSystem GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the ParticleSystem GameObject')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_particle_system_info', params });
            return textResult(r);
        }
    },
    {
        name: 'get_terrain_info',
        description: 'Get Terrain information: size, resolution, layers, tree prototypes, tree instance count',
        schema: {
            instanceId: z.number().optional().describe('Instance ID of the Terrain GameObject'),
            gameObjectPath: z.string().optional().describe('Hierarchical path of the Terrain GameObject')
        },
        handler: async (bridge, params) => {
            const r = await bridge.sendRequest({ method: 'get_terrain_info', params: params ?? {} });
            return textResult(r);
        }
    }
];
export function registerAllTools(server, bridge) {
    for (const tool of tools) {
        server.tool(tool.name, tool.description, tool.schema, async (params) => {
            try {
                return await tool.handler(bridge, params);
            }
            catch (error) {
                const message = error instanceof Error ? error.message : String(error);
                return {
                    content: [{ type: 'text', text: `Error: ${message}` }],
                    isError: true
                };
            }
        });
    }
}
