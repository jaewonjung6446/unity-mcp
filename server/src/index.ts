import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { UnityBridge } from './unity-bridge.js';
import { registerAllTools } from './tools/index.js';

// 명령줄 인자 파싱
function parseArgs(): { port?: number } {
  const args = process.argv.slice(2);
  const result: { port?: number } = {};

  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--port' || args[i] === '-p') {
      const portStr = args[i + 1];
      if (portStr) {
        const port = parseInt(portStr, 10);
        if (!isNaN(port) && port > 0 && port < 65536) {
          result.port = port;
        } else {
          console.error(`[MCP Server] Invalid port: ${portStr}`);
        }
        i++; // skip next arg
      }
    }
  }

  return result;
}

const cliArgs = parseArgs();

const server = new McpServer(
  { name: 'Unity MCP Server', version: '1.0.0' },
  {
    capabilities: { tools: {} },
    instructions: `MCP server for Unity Editor integration.
- When copying a file inside of Assets folder, use the copy_asset tool instead of generic file tools.
- Do not use generic codebase search or file search tools on any files in the Assets folder other than for *.cs files.
- Do not use generic file tools (edit_file, apply, copy, move, etc) when working with anything in the Assets folder.
- When editing an existing scene or prefab, open it first.
- After creating or changing objects in a scene or prefab, focus on the objects that were created or changed.
- After making a change to a scene or prefab that you want to keep, save it.
- After editing a prefab, exit isolation mode before continuing to work on the scene.
- Take a screenshot after every change you make to a loaded Unity scene or prefab that affects visuals.

QA Simulation workflow:
1. Scene analysis: use analyze_scene, find_ui_elements, find_objects_by_criteria, and get_scene_hierarchy to understand scene structure.
2. Code analysis: use get_asset_contents to read key scripts and understand game logic.
3. Plan QA: create a list of test scenarios based on scene and code analysis.
4. Enter Play Mode (set_play_mode) and clear console buffer (get_console_logs with clear=true).
5. For each test step:
   a. Take a screenshot (before state).
   b. Perform action: click_ui_element for UI, simulate_input for gameplay (WASD, mouse, etc.).
   c. Wait for result: wait_for_seconds for animations/transitions.
   d. Verify: get_console_logs (check for errors), get_component_data (check game state), get_scene_hierarchy (check object changes), screenshot (after state).
6. Exit Play Mode (set_play_mode play=false).
7. Write bug report with: severity, reproduction steps, screenshots, console logs, and expected vs actual behavior.`
  }
);

const bridge = new UnityBridge({ port: cliArgs.port });

registerAllTools(server, bridge);

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error('[MCP Server] Started');
  await bridge.start();
}

let isShuttingDown = false;
async function shutdown() {
  if (isShuttingDown) return;
  isShuttingDown = true;
  await bridge.stop();
  await server.close();
  process.exit(0);
}

main().catch((err) => {
  console.error('[MCP Server] Fatal error:', err);
  process.exit(1);
});

process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);
process.stdin.on('close', shutdown);
process.stdin.on('end', shutdown);

process.on('uncaughtException', (error: NodeJS.ErrnoException) => {
  if (error.code === 'EPIPE' || error.code === 'EOF' || error.code === 'ERR_USE_AFTER_CLOSE') {
    shutdown();
    return;
  }
  console.error('[MCP Server] Uncaught exception:', error);
  process.exit(1);
});
