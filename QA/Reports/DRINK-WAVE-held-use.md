# Drink wave held-use slice

Implementation 9720628 uses existing UseHeld F/right mouse input and a cached adapter in the same action driver as Interact. No IHeldItemAction or IInteractable signature changes, no package/input-asset/asmdef changes. Held holds are independent of aim; Escape freezes, physical release/hand change/capture loss/disable cancel; E and F cannot begin together. Prompts display correct E/F and progress.

Unity6000.6.0f1: compile PASS zero C# errors/warnings; full EditMode91/91 and PlayMode25/25, zero failed/skipped. Tests include real keyboard/mouse edges, pause/resume and blocked/identity guards. QA-000 PASS from clean pre-import worktree. Red checkpoint had expected missing BeginHeld compilation failures before implementation. Existing-source metadata prerequisite PR7 required.
