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
│   ├── bin/cli.ts           # CLI 엔트리포인트
│   ├── src/
│   │   ├── index.ts         # MCP 서버 엔트리포인트
│   │   ├── unity-bridge.ts  # WebSocket 클라이언트 (Unity 연결)
│   │   ├── types.ts         # 요청/응답 타입 정의
│   │   └── tools/index.ts   # 91개 도구 정의
│   └── build/               # 컴파일된 JS 출력
└── unity-package/           # Unity 패키지 (C#)
    └── Editor/
        ├── McpServer.cs     # WebSocket 서버 (연결/디스패치)
        ├── McpServerMenu.cs # 에디터 메뉴 통합
        ├── McpSettings.cs   # 설정 관리
        ├── ConsoleLogBuffer.cs  # 콘솔 로그 버퍼
        ├── ShaderGraphHelper.cs # ShaderGraph 유틸리티
        ├── IToolHandler.cs  # 도구 핸들러 인터페이스
        └── Handlers/        # 90개 핸들러 구현
```

## Installation

### 1. Unity Package
`Packages/manifest.json`에 추가:
```json
"com.jaewon.mcp-unity": "https://github.com/jaewonjung6446/unity-mcp.git?path=unity-package"
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

## Available Tools (91)

### Editor Control (7)

| Tool | Description |
|------|-------------|
| `get_state` | 에디터 상태 (플레이 모드, 활성 씬, 플랫폼) |
| `get_selection` | 현재 선택된 오브젝트 |
| `set_selection` | 에디터 선택 변경/해제 (다중 선택, 에셋 선택) |
| `pause_editor` | Play Mode 일시정지/해제 |
| `step_frame` | 일시정지 중 1프레임 전진 |
| `undo_redo` | 실행 취소/다시 실행 (최대 20단계) |
| `clear_console` | 콘솔 창 및 로그 버퍼 클리어 |

### Scene (6)

| Tool | Description |
|------|-------------|
| `open_scene` | 씬 열기 (additive 옵션) |
| `close_scene` | 씬 닫기 |
| `save_scene` | 활성 씬 저장 |
| `get_scenes_list` | 빌드 설정/로드된/전체 씬 목록 |
| `get_scene_hierarchy` | 씬 계층구조 스냅샷 |
| `set_scene_view_camera` | Scene View 카메라 제어 (위치, 회전, 줌, 프리셋 뷰) |

### GameObject (9)

| Tool | Description |
|------|-------------|
| `get_game_object` | GameObject 정보 (ID/이름/경로) |
| `create_game_object` | GameObject 생성 (empty, primitive, 위치/회전/스케일) |
| `delete_game_object` | GameObject 삭제 (Undo 지원) |
| `set_game_object_property` | 속성 설정 (이름, 태그, 레이어, 위치, 회전, 스케일 등) |
| `duplicate_game_object` | 복제 (개수, 오프셋 옵션) |
| `group_game_objects` | 여러 오브젝트를 빈 부모로 그룹화 |
| `align_game_object` | 축 기준 정렬 (min/max/center/first) |
| `set_parent` | 부모-자식 관계 변경/해제 |
| `focus_game_object` | Scene View에서 오브젝트 포커스 |

### Component (5)

| Tool | Description |
|------|-------------|
| `get_component_data` | 컴포넌트 직렬화 필드 값 읽기 |
| `add_component` | 컴포넌트 추가 (Rigidbody, AudioSource 등) |
| `remove_component` | 컴포넌트 제거 |
| `set_component_property` | 컴포넌트 프로퍼티 설정 (int, float, Color, Vector 등) |
| `find_objects_by_criteria` | 태그/레이어/컴포넌트/이름으로 오브젝트 검색 |

### Prefab (3)

| Tool | Description |
|------|-------------|
| `open_prefab` | 프리팹 편집 모드 열기 |
| `create_prefab` | 씬 오브젝트로 프리팹 에셋 생성 |
| `instantiate_prefab` | 프리팹을 씬에 인스턴스화 |

### Asset Management (13)

| Tool | Description |
|------|-------------|
| `create_script` | C# 스크립트 생성 |
| `execute_code` | C# 코드 실행 |
| `get_asset_contents` | 에셋 내용 읽기 |
| `get_asset_importer` | 임포터 설정 조회 |
| `copy_asset` | 에셋 복사 |
| `import_asset` | 에셋 리임포트 |
| `delete_asset` | 에셋/폴더 삭제 |
| `move_asset` | 에셋 이동/이름 변경 |
| `create_folder` | 폴더 생성 (재귀) |
| `get_asset_dependencies` | 에셋 의존성 조회 |
| `find_references` | 에셋 참조 역검색 |
| `get_asset_preview` | 에셋 미리보기 썸네일 이미지 |
| `refresh_assets` | AssetDatabase 새로고침 (Ctrl+R) |

### Search & Validation (4)

| Tool | Description |
|------|-------------|
| `search` | 에셋 또는 씬 오브젝트 검색 |
| `screenshot` | Scene/Game 뷰 스크린샷 캡처 |
| `test_active_scene` | 활성 씬 검증 (누락 스크립트, 머티리얼 등) |
| `get_missing_references` | 씬/에셋에서 누락 참조 스캔 |

### Shader Graph (5)

| Tool | Description |
|------|-------------|
| `create_shader_graph` | ShaderGraph 생성 (URP Lit/Unlit/Canvas 템플릿) |
| `add_shader_graph_node` | 노드 추가 (SampleTexture2D, Color, Multiply 등) |
| `connect_shader_graph_nodes` | 노드 간 연결 |
| `add_shader_graph_property` | 프로퍼티 추가 (Color, Float, Texture2D 등) |
| `get_shader_graph_node_types` | 지원 노드 타입/슬롯 정보 조회 |

### Material & Rendering (7)

| Tool | Description |
|------|-------------|
| `create_material` | 머티리얼 생성 (셰이더 지정) |
| `set_material_property` | 머티리얼 프로퍼티 설정 (color, float, texture 등) |
| `set_game_object_material` | GameObject에 머티리얼 할당 |
| `analyze_scene` | 씬 분석 (렌더러, 머티리얼, 셰이더, 라이트) |
| `get_render_settings` | 렌더 설정 조회 (ambient, fog, skybox, reflections) |
| `set_render_settings` | 렌더 설정 변경 (fog, ambient, skybox 등) |
| `set_light_property` | 라이트 프로퍼티 설정 (타입, 색상, 강도, 그림자 등) |

### Volume / Post-Processing (2)

| Tool | Description |
|------|-------------|
| `get_volume_settings` | URP Volume 설정 조회 (Bloom, Vignette 등) |
| `set_volume_component` | Volume 컴포넌트 설정 (Bloom, ColorAdjustments 등) |

### UI (9)

| Tool | Description |
|------|-------------|
| `create_ui_element` | UI 요소 생성 (Text, Button, Slider 등, Canvas 자동 생성) |
| `find_ui_elements` | Canvas UI 요소 스캔 (경로, 타입, 상태) |
| `inspect_ui_layout` | UI 레이아웃 이슈 검출 (겹침, 화면 밖, 작은 터치 영역) |
| `click_ui_element` | UI 클릭 시뮬레이션 (Play Mode) |
| `set_ui_input` | InputField 텍스트 입력 (Play Mode) |
| `get_ui_state` | UI 요소 상태 조회 (Button, Toggle, Slider 등) |
| `drag_ui_element` | UI 드래그 앤 드롭 시뮬레이션 (Play Mode) |
| `scroll_ui` | 스크롤 이벤트 시뮬레이션 (Play Mode) |
| `set_ui_value` | Slider/Toggle/Dropdown/Scrollbar 값 설정 |

### QA & Play Mode (6)

| Tool | Description |
|------|-------------|
| `set_play_mode` | Play Mode 진입/종료 |
| `get_console_logs` | 콘솔 로그 조회 (error, warning, exception 필터) |
| `wait_for_seconds` | 대기 (0.1~30초) |
| `wait_until` | 조건 충족까지 대기 (오브젝트 활성화, 텍스트 매칭 등) |
| `simulate_input` | 키보드/마우스 입력 시뮬레이션 (Play Mode) |
| `set_time_scale` | Time.timeScale 설정 (슬로모션, 배속) |

### Animation (2)

| Tool | Description |
|------|-------------|
| `get_animator_state` | Animator 상태, 전환 정보, 파라미터 값 조회 |
| `set_animator_parameter` | Animator 파라미터 설정 (Float, Int, Bool, Trigger) |

### Physics (3)

| Tool | Description |
|------|-------------|
| `raycast` | 물리 레이캐스트 (월드/스크린 좌표, 다중 히트) |
| `set_rigidbody_property` | Rigidbody/2D 프로퍼티 설정 (질량, 드래그, 중력 등) |
| `apply_force` | Rigidbody에 힘 적용 (Force, Impulse, VelocityChange) |

### Audio (2)

| Tool | Description |
|------|-------------|
| `get_audio_sources` | 씬 내 모든 AudioSource 정보 조회 |
| `play_audio` | 오디오 재생/정지/일시정지 (클립 변경 가능) |

### Navigation (2)

| Tool | Description |
|------|-------------|
| `get_navmesh_info` | NavMesh 정보, NavMeshAgent/Obstacle 조회 |
| `set_navmesh_destination` | NavMeshAgent 목적지 설정/정지 (Play Mode) |

### Scene Systems (2)

| Tool | Description |
|------|-------------|
| `get_particle_system_info` | ParticleSystem 정보 (모듈 설정, 상태, 파티클 수) |
| `get_terrain_info` | Terrain 정보 (크기, 해상도, 레이어, 트리) |

### Project Info (4)

| Tool | Description |
|------|-------------|
| `get_project_settings` | 프로젝트 설정 조회 (player, quality, physics, time) |
| `get_packages` | 설치된 패키지 목록 |
| `get_build_settings` | 빌드 설정 (타겟 플랫폼, 씬, 개발 모드) |
| `get_performance_stats` | 런타임 성능 통계 (FPS, 메모리, 오브젝트 수) |

## Development

```bash
cd server
npm install
npm run build          # TypeScript 컴파일
npm run watch          # 변경 감지 자동 빌드
node build/cli.js      # 서버 실행
```
