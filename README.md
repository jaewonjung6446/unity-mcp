# Unity MCP

Unity Editor와 Claude Code를 연결하는 MCP (Model Context Protocol) 패키지.

## Architecture

```
Claude Code ←(STDIO)→ Node.js MCP Server ←(WebSocket :8090)→ Unity Editor (C#)
```

## Project Structure

```
unity-mcp/
├── server/                  # Node.js MCP 브릿지 서버 (TypeScript)
│   ├── src/
│   │   ├── index.ts         # MCP 서버 엔트리포인트
│   │   ├── unity-bridge.ts  # WebSocket 클라이언트 (Unity 연결)
│   │   ├── types.ts         # 요청/응답 타입 정의
│   │   ├── tools/index.ts   # 40개 도구 정의
│   │   └── bin/cli.ts       # CLI 엔트리포인트
│   └── build/               # 컴파일된 JS 출력
└── unity-package/           # Unity 패키지 (C#)
    └── Editor/
        ├── McpServer.cs     # WebSocket 서버 (연결/디스패치)
        ├── McpSettings.cs   # 설정 관리
        ├── ConsoleLogBuffer.cs  # 콘솔 로그 버퍼
        ├── ShaderGraphHelper.cs # ShaderGraph 유틸리티
        ├── IToolHandler.cs  # 도구 핸들러 인터페이스
        └── Handlers/        # 39개 핸들러 구현
```

## Installation

### 1. Unity Package
`Packages/manifest.json`에 추가:
```json
"com.jaewon.mcp-unity": "file:../../unity-mcp/unity-package"
```
- Unity 2022.3 이상 필요
- 의존성: `com.unity.nuget.newtonsoft-json`, `com.unity.editorcoroutines`

### 2. MCP Server 빌드
```bash
cd server
npm install
npm run build
```

### 3. MCP Config
`.mcp.json`:
```json
{
  "mcpServers": {
    "unity": {
      "command": "node",
      "args": ["D:\\Unity\\Izakoza\\unity-mcp\\server\\build\\cli.js"]
    }
  }
}
```

## Multiple Unity Projects (다중 프로젝트 설정)

한 로컬에서 여러 Unity 프로젝트의 MCP를 동시에 실행하려면 각각 다른 포트를 사용해야 합니다.

### 1. Unity 프로젝트별 포트 설정
각 Unity 프로젝트의 `ProjectSettings/McpUnitySettings.json`:

**Project A**:
```json
{ "Port": 8090 }
```

**Project B**:
```json
{ "Port": 8091 }
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
1. 명령줄 인자 (`--port`) — 최우선
2. `ProjectSettings/McpUnitySettings.json` 설정 파일
3. 기본값: `8090`

## Available Tools (40)

### Editor & Scene (7)

| Tool | Description |
|------|-------------|
| `get_state` | 에디터 상태 (플레이 모드, 활성 씬, 플랫폼) |
| `open_scene` | 씬 열기 (additive 옵션) |
| `close_scene` | 씬 닫기 |
| `save_scene` | 활성 씬 저장 |
| `get_game_object` | GameObject 정보 (ID/이름/경로) |
| `get_selection` | 현재 선택된 오브젝트 |
| `focus_game_object` | Scene View에서 오브젝트 포커스 |

### Asset (7)

| Tool | Description |
|------|-------------|
| `open_prefab` | 프리팹 편집 모드 열기 |
| `create_script` | C# 스크립트 생성 |
| `execute_code` | C# 코드 실행 |
| `get_asset_contents` | 에셋 내용 읽기 |
| `get_asset_importer` | 임포터 설정 조회 |
| `copy_asset` | 에셋 복사 |
| `import_asset` | 에셋 리임포트 |

### Search & Validation (3)

| Tool | Description |
|------|-------------|
| `search` | 에셋 또는 씬 오브젝트 검색 |
| `screenshot` | Scene/Game 뷰 스크린샷 캡처 |
| `test_active_scene` | 활성 씬 검증 (누락 스크립트, 머티리얼 등) |

### Shader Graph (5)

| Tool | Description |
|------|-------------|
| `create_shader_graph` | ShaderGraph 생성 (URP Lit/Unlit/Canvas 템플릿) |
| `add_shader_graph_node` | 노드 추가 (SampleTexture2D, Color, Multiply 등) |
| `connect_shader_graph_nodes` | 노드 간 연결 |
| `add_shader_graph_property` | 프로퍼티 추가 (Color, Float, Texture2D 등) |
| `get_shader_graph_node_types` | 지원 노드 타입/슬롯 정보 조회 |

### Material (2)

| Tool | Description |
|------|-------------|
| `create_material` | 머티리얼 생성 (셰이더 지정) |
| `set_material_property` | 머티리얼 프로퍼티 설정 (color, float, texture 등) |

### Scene Analysis (2)

| Tool | Description |
|------|-------------|
| `analyze_scene` | 씬 분석 (렌더러, 머티리얼, 셰이더, 라이트) |
| `set_game_object_material` | GameObject에 머티리얼 할당 |

### Volume / Post-Processing (2)

| Tool | Description |
|------|-------------|
| `get_volume_settings` | URP Volume 설정 조회 (Bloom, Vignette 등) |
| `set_volume_component` | Volume 컴포넌트 설정 (Bloom, ColorAdjustments 등) |

### UI QA (5)

| Tool | Description |
|------|-------------|
| `find_ui_elements` | Canvas UI 요소 스캔 (경로, 타입, 상태) |
| `inspect_ui_layout` | UI 레이아웃 이슈 검출 (겹침, 화면 밖, 작은 터치 영역) |
| `click_ui_element` | UI 클릭 시뮬레이션 (Play Mode) |
| `set_ui_input` | InputField 텍스트 입력 (Play Mode) |
| `get_ui_state` | UI 요소 상태 조회 (Button, Toggle, Slider 등) |

### QA Simulation (7)

| Tool | Description |
|------|-------------|
| `set_play_mode` | Play Mode 진입/종료 |
| `get_console_logs` | 콘솔 로그 조회 (error, warning, exception 필터) |
| `get_scene_hierarchy` | 씬 계층구조 스냅샷 |
| `wait_for_seconds` | 대기 (0.1~30초, 서버 측 구현) |
| `simulate_input` | 키보드/마우스 입력 시뮬레이션 (Play Mode) |
| `get_component_data` | 컴포넌트 직렬화 필드 값 읽기 |
| `find_objects_by_criteria` | 태그/레이어/컴포넌트/이름으로 오브젝트 검색 |

## Development

```bash
cd server
npm install
npm run build          # TypeScript 컴파일
npm run watch          # 변경 감지 자동 빌드
node build/cli.js      # 서버 실행
```
