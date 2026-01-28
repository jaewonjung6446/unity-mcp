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

## Multiple Unity Projects (다중 프로젝트 설정)

한 로컬에서 여러 Unity 프로젝트의 MCP를 동시에 실행하려면 각각 다른 포트를 사용해야 합니다.

### 1. Unity 프로젝트별 포트 설정
각 Unity 프로젝트의 `ProjectSettings/McpUnitySettings.json` 파일을 생성:

**Project A** (`ProjectSettings/McpUnitySettings.json`):
```json
{
  "Port": 8090
}
```

**Project B** (`ProjectSettings/McpUnitySettings.json`):
```json
{
  "Port": 8091
}
```

### 2. MCP 서버 설정
`.mcp.json`에서 `--port` 또는 `-p` 인자로 포트 지정:
```json
{
  "mcpServers": {
    "unity-project-a": {
      "command": "node",
      "args": ["D:\\Unity\\unity-mcp\\server\\build\\cli.js", "--port", "8090"]
    },
    "unity-project-b": {
      "command": "node",
      "args": ["D:\\Unity\\unity-mcp\\server\\build\\cli.js", "--port", "8091"]
    }
  }
}
```

### 포트 우선순위
1. 명령줄 인자 (`--port`) - 최우선
2. `ProjectSettings/McpUnitySettings.json` 설정 파일
3. 기본값: 8090

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
