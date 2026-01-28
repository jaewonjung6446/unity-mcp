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
