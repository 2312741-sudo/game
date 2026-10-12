# Trạm Chanh — Vietnamese Roadside Lemon Tea Simulator

Unity 6 (URP) project. The Unity project root is this repository root.

| Start here | |
|---|---|
| Architecture & ground truth | `Docs/ARCHITECTURE.md` |
| Systems | `Docs/ORDER_SYSTEM.md`, `Docs/INTERACTION_SYSTEM.md`, `Docs/DRINK_WORKFLOW.md`, `Docs/CAKE_WORKFLOW.md` |
| Assets | `Docs/ASSET_INTEGRATION.md`, `ArtSource/README.md` |
| Tasks & status | `Docs/PROJECT_TASK_PLAN.md` (§0.1 status board) |
| Conventions | `Docs/CODING_CONVENTIONS.md` |
| Source documents | `Docs/Reference/` |

## First open

1. Open the folder with **Unity 6000.6.0f1** exactly (`ProjectSettings/ProjectVersion.txt`). The package versions in `Packages/manifest.json` and `Packages/packages-lock.json` are resolved for that editor.
2. `TramChanhProjectSetup` runs automatically: physics layers, linear colour space, URP asset (`Assets/TramChanh/Settings/Rendering/`). Re-run via **Tram Chanh ▸ Setup ▸ Apply Project Settings**.
3. **Tram Chanh ▸ Placeholders ▸ Build Stall + New Sign Placeholder** generates `PF_Stall_TramChanh` and `PF_Sign_TramChanh_New`.
4. **Window ▸ General ▸ Test Runner**: run EditMode and PlayMode.
5. Commit the generated `.meta` files, `ProjectSettings/*`, `Packages/packages-lock.json` and the generated prefabs/materials.

## QA without Unity

```bash
python3 Automation/qa000_repo_check.py --report QA/Reports/QA-000-<date>.md
```
