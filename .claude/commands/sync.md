Sync the unity-package/Editor directory from this repo to the two other projects.

Source: `D:\Unity\Izakoza\unity-mcp\unity-package\Editor\`

Targets:
- `D:\fork\unity-mcp\unity-package\Editor\`
- `D:\Unity\CautionPotion\unity-mcp\unity-package\Editor\`

Steps:
1. Use robocopy to mirror the source Editor folder to each target (robocopy /MIR copies new/changed files and deletes files that no longer exist in source).
2. Report which files were copied/deleted.
