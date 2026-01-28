import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { UnityBridge } from './unity-bridge.js';
import { registerAllTools } from './tools/index.js';
const server = new McpServer({ name: 'Unity MCP Server', version: '1.0.0' }, {
    capabilities: { tools: {} },
    instructions: `MCP server for Unity Editor integration.
- When copying a file inside of Assets folder, use the copy_asset tool instead of generic file tools.
- Do not use generic codebase search or file search tools on any files in the Assets folder other than for *.cs files.
- Do not use generic file tools (edit_file, apply, copy, move, etc) when working with anything in the Assets folder.
- When editing an existing scene or prefab, open it first.
- After creating or changing objects in a scene or prefab, focus on the objects that were created or changed.
- After making a change to a scene or prefab that you want to keep, save it.
- After editing a prefab, exit isolation mode before continuing to work on the scene.
- Take a screenshot after every change you make to a loaded Unity scene or prefab that affects visuals.`
});
const bridge = new UnityBridge();
registerAllTools(server, bridge);
async function main() {
    const transport = new StdioServerTransport();
    await server.connect(transport);
    console.error('[MCP Server] Started');
    await bridge.start();
}
let isShuttingDown = false;
async function shutdown() {
    if (isShuttingDown)
        return;
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
process.on('uncaughtException', (error) => {
    if (error.code === 'EPIPE' || error.code === 'EOF' || error.code === 'ERR_USE_AFTER_CLOSE') {
        shutdown();
        return;
    }
    console.error('[MCP Server] Uncaught exception:', error);
    process.exit(1);
});
