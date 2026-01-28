# Unity MCP

Custom MCP (Model Context Protocol) package for Unity Editor integration.

## Architecture

```
Claude Code ←(STDIO)→ Node.js MCP Server ←(WebSocket :8090)→ Unity Editor (C#)
```

## Components

### `server/` - npm MCP Server
Node.js TypeScript server that bridges MCP protocol (STDIO) to Unity's WebSocket server.

### `unity-package/` - Unity Package
C# WebSocket server running inside Unity Editor that executes tool commands.

## Installation

### 1. Unity Package
Add to `Packages/manifest.json`:
```json
"com.jaewon.mcp-unity": "file:../../unity-mcp/unity-package"
```

### 2. MCP Config
In `.mcp.json`:
```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["D:\\Unity\\CautionPotion\\unity-mcp\\server\\build\\cli.js"]
    }
  }
}
```

## Available Tools (17)

| Tool | Description |
|------|-------------|
| `get_state` | Editor state (play mode, scene, platform) |
| `get_game_object` | GameObject info by ID/name/path |
| `get_selection` | Currently selected objects |
| `focus_game_object` | Focus Scene View on object |
| `open_scene` | Open a scene |
| `close_scene` | Close a scene |
| `save_scene` | Save active scene |
| `open_prefab` | Open prefab for editing |
| `create_script` | Create C# script |
| `execute_code` | Execute C# code |
| `get_asset_contents` | Read asset contents |
| `get_asset_importer` | Get importer settings |
| `copy_asset` | Copy an asset |
| `import_asset` | Reimport an asset |
| `search` | Search assets or scene objects |
| `screenshot` | Capture screenshot |
| `test_active_scene` | Validate active scene |

## Development

```bash
cd server
npm install
npm run build
```
