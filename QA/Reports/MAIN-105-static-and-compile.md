# MAIN-105 — Static pre-flight and editor QA compile check

Branch `feature/MAIN-105-qa-validation` (base `248eac1`). Container without Unity. **Neither section is a Unity result.**

## 1. Static pre-flight (`Automation/static_preflight.py`)

Stdlib Python, read-only. Scans every `.unity`, `.prefab`, `.asset` under `Assets/TramChanh` and checks:
GUID references resolve to a `.meta` in `Assets/` (or an embedded package) or a Unity built-in GUID; local
`{fileID: N}` references resolve to an object in the same file; `{fileID, guid, type: 2}` references into project YAML
files resolve to an object in the target; no duplicate `.meta` GUIDs; plus orphan `.meta`/missing `.meta` warnings.
Unresolved GUIDs FAIL unless listed in `--allow-external` (registry packages are not in the repo).

Output on this branch (after adding the QA editor files and their `.meta`s):

```
MAIN-105 static pre-flight (STATIC CHECK - not a Unity result)
root: /home/user/wt-qa
scope: Assets/TramChanh
meta GUIDs indexed: 442 (duplicates: 0)
scanned files: 49, objects: 1996, references: 14133
  null {fileID: 0}: 8471
  local fileID refs: 3806
  guid refs: 1856 (resolved in repo: 1523, built-in: 333, unresolved: 0 in 0 GUIDs)
  cross-file fileIDs checked: 351
FAIL: 0
WARN: 0
INFO: 0
STATIC PREFLIGHT: PASS
exit=0
```

Self-test (scratch copy, deleted afterwards): one broken local fileID in `PF_Grill_Elmich.prefab`, one bogus script
GUID and one bad cross-file fileID in `SCN_TramChanh_Main.unity`, one duplicated `.cs.meta` GUID. The checker reported
exactly those 4 FAILs (`DUPLICATE_GUID`, `LOCAL_FILEID_MISSING`, `CROSS_FILEID_MISSING`, `UNRESOLVED_GUID`) and exited 1.

Limits: it cannot see Library/PackageCache, so it cannot prove that a package GUID exists (none are referenced
today); it does not check fileIDs inside binary/model assets (`type: 3` into `.fbx`); it does not know whether a
script GUID's class still matches the serialized fields. Unity's import is the authority.

## 2. Editor QA menu compile check

File: `Assets/TramChanh/Scripts/Editor/QA/TramChanhValidationMenu.cs` (guid `946bec250da14b8dad0eac9660971728`,
folder `QA.meta` guid `f1730aac0e8a408ca92251c3f1a9929f`), assembly `TramChanh.Editor`.

Compile setup (scratchpad `r05/rt`, `r05/ed`, not in the repo):

1. `mainc/c.csproj` copy → all runtime scripts as one `c.dll` against UnityEngine **2021.3.33** module reference DLLs
   (`refs_dl/unityengine.modules`) + Input System stand-in: 0 errors, 0 warnings.
2. `ed.csproj` (`AssemblyName=TramChanh.Editor`, `UNITY_EDITOR`, C# 9, netstandard2.1) compiling
   `TramChanhValidationMenu.cs` + `ProjectSetup/TramChanhSceneMenu.cs` (for the shared scene-path constant) against
   `c.dll`, the 2021.3.33 engine modules and **`UnityEditor.dll` from NuGet `Unity3D.SDK` 2021.1.14.1**:
   **0 errors, 0 warnings.**

Why 2021.1 for UnityEditor: `refs_dl/unityeditor` (NuGet `UnityEditor` 2019.3.2.1) contains no DLL at all — only
a nuspec and licence. `Unity3D.SDK` 2021.1.14.1 was the newest editor reference available from nuget.org; it was
used only as a compile reference, never executed.

### API notes: 2021.x references vs Unity 6000.6

| API | Status | What the file does |
|---|---|---|
| `Object.FindObjectsOfType` / `FindObjectOfType` | Obsolete since Unity 2023.1 | Not used; scenes are walked from `Scene.GetRootGameObjects()` + `GetComponentsInChildren(true)` |
| `SerializedProperty.objectReferenceInstanceIDValue` | Unity 6.x is migrating instance IDs to `EntityId` (`objectReferenceEntityIdValue`); the int property may be obsolete or removed in 6000.6 (not verifiable here) | Read **by reflection**: InstanceID property first, else EntityId property; if neither exists the References check emits a WARNING instead of failing silently |
| Build Profiles (`UnityEditor.Build.Profile.BuildProfile`) | Unity 6 only | Read by reflection (`GetActiveBuildProfile`, `overrideGlobalScenes`, `scenes`); if absent, only `EditorBuildSettings.scenes` is reported |
| `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount`, `PrefabUtility.IsPrefabAssetMissing`, `EditorSceneManager.OpenScene(Additive)`/`CloseScene(scene, true)`, `AssetDatabase.FindAssets/LoadAssetAtPath/IsValidFolder`, `EditorBuildSettings.scenes` (get only), `Application.logMessageReceived`, `EditorUtility.scriptCompilationFailed`, `EditorApplication.Exit` | Present in 2021.3 and Unity 6 | Used directly |
| `SerializedObject` as `IDisposable` | 2021.3 and 6 | `using` blocks |

Read-only guarantees, checked by grep on the file: no `SaveScene`, `SaveAssets`, `SaveAsPrefabAsset`,
`SetDirty`, `MarkSceneDirty`, `SaveCurrentModifiedScenesIfUserWantsTo` or `EditorBuildSettings.scenes =`. A scene
that is not already open is opened additively and closed with `removeScene: true` (no save prompt, no save).
An already open scene is inspected in memory and left open; if it is dirty the report says the in-memory version
was checked.

Menu note: Unity treats `/` in a `MenuItem` path as a submenu, so "Validate Prefab/Scene References" is spelled
**"Validate Prefab and Scene References"**.

## 3. Not verified (needs the Mac)

- That `TramChanh.Editor` compiles in Unity 6000.6.0f1 with the real asmdef graph and URP/Input System packages.
- Exact Unity 6000.6 behaviour of the reflected properties (`objectReferenceInstanceIDValue` / `objectReferenceEntityIdValue`, `BuildProfile`).
- The validators' findings on the real project (expected from static reading: bootstrap fields all assigned, the
  five environment anchors present in `PF_AccelRoadsideEnvironment.prefab`).
- Whether opening scenes additively in batch mode logs warnings that the Console capture then counts.
