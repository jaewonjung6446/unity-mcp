import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { UnityBridge } from '../unity-bridge.js';
import { z } from 'zod';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

type ToolDef = {
  name: string;
  description: string;
  schema: Record<string, z.ZodTypeAny>;
  handler: (bridge: UnityBridge, params: any) => Promise<CallToolResult>;
};

function textResult(response: any): CallToolResult {
  return {
    content: [{
      type: 'text',
      text: typeof response === 'string' ? response : JSON.stringify(response, null, 2)
    }]
  };
}

function imageResult(response: any): CallToolResult {
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

const tools: ToolDef[] = [
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
      templateType: z.enum(['urp_lit', 'urp_unlit']).optional().describe('Template type: "urp_lit" (default) or "urp_unlit"'),
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
  }
];

export function registerAllTools(server: McpServer, bridge: UnityBridge): void {
  for (const tool of tools) {
    server.tool(
      tool.name,
      tool.description,
      tool.schema,
      async (params: any) => {
        try {
          return await tool.handler(bridge, params);
        } catch (error) {
          const message = error instanceof Error ? error.message : String(error);
          return {
            content: [{ type: 'text', text: `Error: ${message}` }],
            isError: true
          };
        }
      }
    );
  }
}
