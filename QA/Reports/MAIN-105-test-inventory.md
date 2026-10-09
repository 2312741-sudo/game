# MAIN-105 — Test inventory and harness coverage

Branch `feature/MAIN-105-qa-validation`, based on `wave/MAIN-001-integration` @ `248eac1` (draft PR #22).
Author: CLAUDE-05 (QA package). Written in a Linux container with **no Unity Editor**.

> **No result in this file is a Unity result.** Lines marked "harness" are .NET 8 + NUnitLite runs of the
> real repo test files compiled against hand-written stand-ins for UnityEngine. A static check is a third,
> separate kind of evidence. Only a run in Unity 6000.6.0f1 (see `Docs/QA/UNITY_VALIDATION.md`) can
> produce a Unity PASS.

## 1. Categories

| Category | Meaning |
|---|---|
| **pure-logic** | Needs no Unity object lifecycle: plain C# domain code. At most a few Unity type names (`Transform` as a null return type, `[SerializeField]`) that a stand-in can satisfy. Can run in a .NET harness; the harness result is still not a Unity result. |
| **real Unity EditMode** | Edit Mode test that creates `GameObject`s, components or `ScriptableObject`s, relies on Unity null semantics, `OnEnable`/`OnDestroy`, `Renderer.bounds`, or an Editor-assembly builder. It loads no saved asset file. A harness can only approximate it with stand-ins. |
| **real Unity PlayMode** | Lives in `TramChanh.Tests.PlayMode`, uses `[UnityTest]` coroutines, frames, physics or runtime scene loading. Flag "+AD" means it also loads saved assets/scenes. |
| **needs AssetDatabase/scene (Unity only)** | Reads saved `.asset`/`.prefab`/`.unity` files or project settings through `AssetDatabase`, `EditorSceneManager`, `PrefabUtility`, `LayerMask`/TagManager or `ProjectSettings.asset`. Cannot run outside Unity at all. |
| **support** | No test methods: fakes, kits, shared assertions, probes. Listed with the category of what it needs. |
| **static check** (separate) | Not a test file: `Automation/static_preflight.py` (MAIN-105), `Automation/qa000_repo_check.py` (QA-000) and the earlier scratchpad YAML GUID checks. They read text files only. |

## 2. Counts

57 `.cs` files under `Assets/TramChanh/Tests/**`: 48 test files and 9 support files.

| Category | Test files | Support files |
|---|---:|---:|
| pure-logic | 17 | 7 |
| real Unity EditMode | 12 | 1 (`LobbyTestKit`) |
| needs AssetDatabase/scene (Unity only) | 9 | 0 |
| real Unity PlayMode | 10 (6 of them +AD) | 1 (`ServiceScopeProbe`) |
| **Total** | **48** | **9** |

Of the 48 test files, **19 have been executed in a .NET harness** (8 pure-logic, 10 real Unity EditMode through
stand-ins, 1 PlayMode through stand-ins). **0 have been executed in Unity** by any agent.

## 3. File-by-file

Columns: `[Test]/[UnityTest]` = attribute lines, `[TestCase]` = attribute lines (from a grep; NUnit case count is in §4), Harness = which scratchpad harness compiled and ran it.

### EditMode (`TramChanh.Tests.EditMode`, Editor-only asmdef, `UNITY_INCLUDE_TESTS`)

| File | Category | [Test] | [TestCase] | Harness | Notes |
|---|---|---:|---:|---|---|
| Cakes/CakeDomainTests.cs | real Unity EditMode | 12 | 3 | m3/h | GameObjects + ScriptableObjects |
| Cakes/CakeSavedAssetTests.cs | needs AssetDatabase/scene | 4 | 0 | — | LoadAssetAtPath on saved cake assets |
| Core/CoreValueTests.cs | pure-logic | 3 | 8 | — | |
| Core/EventBusTests.cs | pure-logic | 7 | 3 | — | |
| Core/GameClockTests.cs | pure-logic | 1 | 3 | — | |
| Core/ServiceRegistryTests.cs | pure-logic | 3 | 0 | — | |
| Drinks/DrinkAdapterTests.cs | real Unity EditMode | 9 | 3 | m002 | |
| Drinks/DrinkPreparationTests.cs | pure-logic | 9 | 5 | m002 | |
| Drinks/DrinkReadyHandoffTests.cs | real Unity EditMode | 3 | 0 | m002 | |
| Drinks/TeaRackPickupAssetTests.cs | needs AssetDatabase/scene | 2 | 0 | excluded from m002 | saved rack prefab |
| Drinks/TeaRackPickupTests.cs | pure-logic | 4 | 3 | m002 | |
| Drinks/TeaRackReadyGateTests.cs | real Unity EditMode | 4 | 0 | m002 | |
| GroundTruth/StallDimensionsTests.cs | pure-logic | 1 | 0 | — | |
| Integration/DrinkWaveAssetTests.cs | needs AssetDatabase/scene | 4 | 0 | — | drink-wave scene/prefabs |
| Interaction/HeldItemEventTests.cs | pure-logic | 1 | 0 | — | `Transform` only as null return |
| Interaction/HeldItemViewTests.cs | real Unity EditMode | 1 | 0 | — | transforms/parenting |
| Interaction/HeldUseDriverTests.cs | pure-logic | 12 | 4 | — | `Transform` only as null return |
| Interaction/InteractionDriverTests.cs | pure-logic | 6 | 5 | — | `Transform` only as null return |
| Interaction/PlayerInputSetupTests.cs | needs AssetDatabase/scene | 1 | 0 | — | reads ProjectSettings.asset |
| Interaction/PlayerInteractionSceneTests.cs | needs AssetDatabase/scene | 5 | 0 | — | opens scene, missing-script count |
| Lobby/DeliveryAdapterTests.cs | real Unity EditMode | 16 | 6 | m4 | |
| Lobby/FakeOrderService.cs | support (pure-logic) | 0 | 0 | m4 | |
| Lobby/LobbyOrderControllerTests.cs | real Unity EditMode | 15 | 0 | m4 | |
| Lobby/LobbyTestKit.cs | support (real Unity EditMode) | 0 | 0 | m4 | creates GameObjects |
| Lobby/MixedOrderLobbyFlowTests.cs | real Unity EditMode | 1 | 0 | m4 | |
| Lobby/OrderPointTests.cs | real Unity EditMode | 15 | 0 | m4 | |
| Orders/DeliveryContractAssertions.cs | support (pure-logic) | 0 | 0 | m4 | |
| Orders/FaultyPreparedContractFake.cs | support (pure-logic) | 0 | 0 | m4 | |
| Orders/MixedOrderFlowTests.cs | pure-logic | 2 | 4 | m4 | |
| Orders/OrderContractTests.cs | pure-logic | 10 | 9 | m4 | ARCH001 checks assembly names — see §5 |
| Orders/OrderDeliveryTests.cs | pure-logic | 8 | 15 | m4 | |
| Orders/OrderGameplayTests.cs | pure-logic | 17 | 0 | m4 | uses `Is.Not.AllocatingGCMemory()` (stand-in in m4) |
| Orders/OrderTransactionTests.cs | pure-logic | 8 | 0 | m4 | |
| Orders/PreparedContractFault.cs | support (pure-logic) | 0 | 0 | m4 | |
| Orders/PreparedItemContractAssertions.cs | support (pure-logic) | 0 | 0 | m4, m002 | |
| Orders/PreparedItemContractFake.cs | support (pure-logic) | 0 | 0 | m4 | |
| Orders/StallTicketContractFake.cs | support (pure-logic) | 0 | 0 | m4 | |
| Placeholders/AccelRoadsideSceneTests.cs | needs AssetDatabase/scene | 7 | 0 | — | |
| Placeholders/GameplayBlockoutTests.cs | needs AssetDatabase/scene | 7 | 0 | — | |
| Placeholders/StallPlaceholderTests.cs | real Unity EditMode | 9 | 0 | — | uses Editor `StallPlaceholderBuilder`, renderer bounds |
| Ready/ReadyAdapterTests.cs | real Unity EditMode | 18 | 2 | m002 | |
| Ready/ReadyRealServiceTests.cs | real Unity EditMode | 9 | 0 | m002 | |
| Setup/ProjectSetupTests.cs | needs AssetDatabase/scene | 3 | 0 | — | layers, URP asset, colour space |
| Smoke/EditModeSmokeTests.cs | pure-logic | 1 | 0 | — | |
| UI/OrderEntryModelTests.cs | pure-logic | 12 | 0 | m4 | |
| UI/OrderEntryUITests.cs | needs AssetDatabase/scene | 6 | 0 | — | loads saved localization table |

### PlayMode (`TramChanh.Tests.PlayMode`, all platforms, `UNITY_INCLUDE_TESTS`)

| File | Category | [UnityTest] | Harness | Notes |
|---|---|---:|---|---|
| Cakes/CakeStationFlowTests.cs | real Unity PlayMode +AD | 8 | m3/h (custom coroutine driver) | `#if UNITY_EDITOR`, LoadAssetAtPath |
| Core/CoreServicesSmokeTests.cs | real Unity PlayMode | 2 | — | SceneManager.CreateScene/UnloadSceneAsync |
| Core/ServiceScopeProbe.cs | support (PlayMode) | 0 | — | MonoBehaviour probe |
| Drinks/TeaRackPickupPlayModeTests.cs | real Unity PlayMode +AD | 5 | — | |
| Integration/DrinkWaveFlowTests.cs | real Unity PlayMode +AD | 5 | — | |
| Integration/TramChanhMainSceneTests.cs | real Unity PlayMode +AD | 2 | — | loads SCN_TramChanh_Main (compiled only, mainc/t.csproj) |
| Interaction/PlayerInteractionTests.cs | real Unity PlayMode +AD | 18 | — | Input System test framework |
| Lobby/DeliveryPresentationTests.cs | real Unity PlayMode | 3 | — | |
| Placeholders/GameplayBlockoutSmokeTests.cs | real Unity PlayMode +AD | 1 | — | |
| Ready/ReadyPresentationTests.cs | real Unity PlayMode | 3 | — | |
| Smoke/PlayModeSmokeTests.cs | real Unity PlayMode | 1 | — | |

## 4. Harnesses: what each one ran and how it differs from Unity

All three were re-run in this task against this branch (`/home/user/wt-qa`, same runtime/test sources as `248eac1`); the
original runs used the agent worktrees `wt-drink`, `wt-cake`, `wt-orders`. Copies live in the session scratchpad
(`r05/m002`, `r05/m3h`, `r05/single`, `r05/multi`), never in the repo.

| Harness | Real test files compiled and run | Cases | Result (harness, not Unity) |
|---|---|---:|---|
| **m002** (drinks) | EditMode/Drinks: DrinkAdapterTests, DrinkPreparationTests, DrinkReadyHandoffTests, TeaRackPickupTests, TeaRackReadyGateTests (TeaRackPickupAssetTests excluded); EditMode/Ready: ReadyAdapterTests, ReadyRealServiceTests; helper Orders/PreparedItemContractAssertions | 69 | 69/69 |
| **m3/h** (cakes) | EditMode/Cakes/CakeDomainTests (20 cases); PlayMode/Cakes/CakeStationFlowTests (8 `[UnityTest]`, driven by `Harness.PlayModeCakeRunner`) | 28 | 28/28 |
| **m4** (orders/lobby) | EditMode/UI/OrderEntryModelTests; EditMode/Orders/*.cs (5 test files + 6 helpers); EditMode/Lobby/*.cs (4 test files + 2 helpers) | 181 | 180/181 single assembly; **181/181** one assembly per asmdef (§5) |

Runtime sources compiled into each harness: Core (minus `UnityGameClock`), Core/Results, `TramChanhLayers`, a subset of
Core/Content, Orders, 12 Interaction files (no Input System, no player), plus Drinks/Lobby/`ReadyCounterPoint` (m002),
Cakes/`ReadyCounterPoint` (m3/h) or Lobby/`OrderEntryModel` (m4).

How the stand-ins differ from Unity (applies to every harness result):

- **UnityEngine is a hand-written stub** (`UnityStub.cs`, 140–170 lines): `Object` null semantics via a `Dead` flag,
  `GameObject`/`Component`/`Transform` with simple parenting, `AddComponent` calls `OnEnable` by reflection,
  `Destroy` == `DestroyImmediate` (no end-of-frame deferral), no `Awake`/`Start`/`Update` loop, no physics, no
  rendering, `Quaternion` multiplication is the identity, `Debug.LogError` feeds a stub `LogAssert`.
- **Single assembly** (m002, m3/h, m4 as built): every source file and test is compiled into one assembly (`t` or
  `TramChanh.Tests.PlayMode`), so `internal` visibility, asmdef reference rules and assembly names are not exercised.
  `InternalsVisibleTo` files (`AssemblyInfo.cs`) are excluded.
- **.NET 8 / CoreCLR, not Mono/IL2CPP**; primitives live in `System.Private.CoreLib`, not `mscorlib`; GC and allocation
  behaviour differ. m4's `AllocatingGCMemory` constraint is re-implemented with `GC.GetAllocatedBytesForCurrentThread`,
  not Unity's profiler-based recorder.
- **m3/h `AssetDatabase`** is a lookup that returns hand-built objects whose scalar fields are read from the saved YAML;
  structure is mirrored by hand, so it does not prove the saved prefab is what Unity deserializes. `[UnityTest]`
  coroutines are drained synchronously: `yield return null` advances no frame.
- **m002/m4 `SerializedObject`** writes private fields by reflection; Unity's serialization rules are not applied.
- `UNITY_EDITOR`/`UNITY_INCLUDE_TESTS` are defined only in m3/h.

Compile-only evidence (separate from harness runs): `scratchpad/mainc/c.csproj` compiles all runtime scripts against
UnityEngine 2021.3.33 module reference DLLs with an Input System stand-in; `mainc/t.csproj` additionally compiles
`TramChanhMainSceneTests.cs`. Reference DLLs are not Unity 6 and nothing is executed.

## 5. The 180/181 issue — `OrderContractTests.ARCH001_PublicContractsReferenceOnlyCoreOrdersAndSystemTypes`

The test walks the public methods of `IOrderService`, `IStallTicketQueue`, `IReadyShelfPlacement`,
`IReadyShelfPickup` and `IPreparedItem` and asserts that every parameter/return type comes from an assembly named
`TramChanh.Core`, `TramChanh.Orders`, `mscorlib` or `System*` (`OrderContractTests.cs` line 161–166).

**Cause.** The m4 harness compiles everything into one assembly named `t`, so `typeof(IReadOnlyOrder).Assembly.GetName().Name`
is `"t"`; the first boundary type checked fails:

```
1) Failed : TramChanh.Tests.EditMode.Orders.OrderContractTests.ARCH001_PublicContractsReferenceOnlyCoreOrdersAndSystemTypes
  TramChanh.Orders.IReadOnlyOrder
  Expected: True
  But was:  False
   at ...OrderContractTests.AssertBoundary(Type type) in .../OrderContractTests.cs:line 164
  Test Count: 181, Passed: 180, Failed: 1
```

**Proof (multi-assembly variant).** `scratchpad/r05/multi/gen.py` generates one csproj per asmdef for exactly the
sources m4 compiles — `UnityEngine` (the same stub, minus the test-runner glue), `TramChanh.Core`, `TramChanh.Content`,
`TramChanh.Orders`, `TramChanh.Interaction`, `TramChanh.Lobby`, `TramChanh.UI` and `TramChanh.Tests.EditMode` — with
`ProjectReference`s read from each `.asmdef` `references` array, `DisableTransitiveProjectReferences=true` (Unity
asmdef references are not transitive) and the repo `AssemblyInfo.cs` files included (`InternalsVisibleTo`).
References to asmdefs with no sources in this subset (Input System, Stall, Drinks, Cakes, Customers, App, DevTools,
Editor, TestRunner, URP) are omitted. It builds with 0 errors and:

```
$ dotnet TramChanh.Tests.EditMode.dll
  Overall result: Passed
  Test Count: 181, Passed: 181, Failed: 0
$ dotnet TramChanh.Tests.EditMode.dll --where "test =~ ARCH001"
  Test Count: 2, Passed: 2, Failed: 0
```

The test file and every source file are unchanged; nothing is skipped. Conclusion: ARCH001 fails in m4 only because
of the single-assembly build; the contract types do live in `TramChanh.Core`/`TramChanh.Orders` when the asmdef layout
is reproduced. Side result: the 8-assembly build also shows that this subset compiles with Unity's non-transitive
asmdef references and `InternalsVisibleTo("TramChanh.Tests.EditMode")`. This is still a harness result; the Unity
EditMode run is the authority.

## 6. Not covered by any harness

29 of 48 test files have never been executed anywhere (only compiled, where the compile harness covers them): all 9
"needs AssetDatabase/scene" files, 9 of 10 PlayMode files, the EditMode files `HeldItemViewTests` and
`StallPlaceholderTests`, and 9 pure-logic files (`Core/CoreValueTests`, `EventBusTests`, `GameClockTests`,
`ServiceRegistryTests`, `StallDimensionsTests`, `EditModeSmokeTests`, `HeldItemEventTests`, `HeldUseDriverTests`,
`InteractionDriverTests`) that a harness could run but that were outside the earlier waves' scope. They are covered
only by the Unity runs in `Docs/QA/UNITY_VALIDATION.md`.
