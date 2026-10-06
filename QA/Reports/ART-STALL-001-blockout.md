# ART-STALL-001 — primitive gameplay blockout

Open `Assets/TramChanh/Scenes/Gameplay/SCN_Gameplay_Blockout.unity` and enter Play to inspect the static overview camera. The editor menu `Tram Chanh > Placeholders > Create Gameplay Blockout Scene` creates missing assets or reopens the existing scene, preserving artist edits.

The saved stall structure measures 1.8 m wide × 0.8 m deep × 2.2 m tall at root scale one. Counter surfaces are exactly 1.0 m above ground. The scene includes the NEW sign placeholder only, all seven requested equipment/furniture prefabs, a 10 m primitive ground plane, lighting, and a 1.7 m capsule reference.

## Validation

- Unity 6000.6.0f1: compile successful; no C# compiler errors or warnings.
- EditMode: 21/21 passed, including seven saved-blockout tests.
- PlayMode: 2/2 passed, including saved-scene loading and ten frames without unexpected Console messages.
- Manual Play in the user's working folder: Console counters show zero errors and zero warnings.
- Saved-asset checks cover exact stall/counter scale, equipment bottom pivots, scene prefab instances, canonical anchor IDs, unconfirmed positions, required equipment/furniture anchors, rear grill hinge, primitive meshes/colliders, materials and missing scripts, and flush recessed bins.
- The initial geometry test detected the topping cover rotating below the prefab pivot; reversing the hinge rotation fixed it and the full suite passed.
- QA-000 docs, layers/constants naming, assembly reference graph, cycle checks and old-sign checks pass. Its workspace-cleanliness criterion fails in the user's working folder because pre-existing Unity package/version changes and generated files are intentionally preserved. Those changes are excluded from this PR.

## Known limitations

- Repository version remains pinned to Unity 6000.0.40f1. Only 6000.6.0f1 is installed, so the pinned version has not been tested. Automated tests used a disposable copy with the installed editor/package versions; no package/version upgrade is included.
- Equipment dimensions, sign dimensions, furniture placement and station positions are provisional editor seeds, awaiting measured reference (DEC-011). All stall anchors remain unconfirmed.
- `ToppingStationAnchor` and `WrappingAnchor` are scene instance name overrides for the requested labels. Their IDs remain `Topping` and `Wrap`; the canonical stall prefab retains `ToppingAnchor` and `WrapAnchor`. The other canonical anchors are retained.
- Primitive counter openings and the lower cabinet/body collider are scene instance overrides for visual scale inspection. They are provisional and do not define a final measured layout.
- This scene supports Play mode visual inspection with a static camera. Player movement, service bootstrap, production, ordering, UI and final art are future tasks.
- The topping cover, grill plates and lid hinge are separate transforms; no animation or production controllers are added. The ready counter is an anchor/trigger prefab without a dedicated mesh, as documented.
- The previously modified Untitled editor scene was preserved locally as `Assets/SCN_Local_PreBlockout.unity`, excluded from the PR.

## Scene hierarchy

```text
SCN_Gameplay_Blockout
  Environment
    GroundPlane
  PF_Stall_TramChanh
    Structure
      SM_Stall_Base
    Counter
      SM_Stall_Counter
      SM_Counter_Front
      SM_Counter_Back
      SM_Counter_Left
      SM_Counter_Right
    Frame
      SM_Stall_Frame_Post0
      SM_Stall_Frame_Post1
      SM_Stall_Frame_Post2
      SM_Stall_Frame_Post3
    Roof
      SM_Stall_Roof
    Sign
      PF_Sign_TramChanh_New
        SM_Sign_TramChanh_New
        SM_Sign_TramChanh_New_TopCap
    Lights
    Wheels
      SM_Stall_CasterWheel0
      SM_Stall_CasterWheel1
      SM_Stall_CasterWheel2
      SM_Stall_CasterWheel3
    Colliders
      Body
      Roof
      Post0
      Post1
      Post2
      Post3
    Anchors
      TeaRackAnchor
        PF_RedTeaRack
          Rack
            Bottom
            Side
            End
            Side
            End
          InteractionPoint
          OutputPoint
          BagSlots
            Slot01
            Slot02
      ToppingStationAnchor
        PF_ToppingStation
          StationFrame
          CoconutJellyBin
            Bin
              Bottom
              Side
              End
              Side
              End
            InteractionPoint
          LemonJellyBin
            Bin
              Bottom
              Side
              End
              Side
              End
            InteractionPoint
          CoverPivot
            TransparentCover
      IceBinAnchor
        PF_IceBin
          Bin
            Bottom
            Side
            End
            Side
            End
          IceVolume
          InteractionPoint
          ScoopRestPoint
      WipeAreaAnchor
      GrillAnchor
        PF_Grill_Elmich
          Base
          LowerPlate
          LidPivot
            Lid
            UpperPlate
            Handle
          Display
          Anchors
            InteractionPoint
            CakePlacementPoint
            SpatulaPoint
            AudioPoint
      BatterAreaAnchor
      RollAreaAnchor
      SauceAnchor
      WrappingAnchor
      ReadyCounterAnchor
        PF_ReadyCounterPoint
          InteractionTrigger
          DrinkPlacement
          CakePlacement
          OrderIndicator
  CustomerArea
    PF_YellowCrateTable
      Crate
      TrayTop
      InteractionPoint
      DeliveryPoint
      Seats
        Seat01
        Seat02
    PF_PlasticStool
      Seat
      Leg
      Leg
      Leg
      Leg
      SeatPoint
    PF_PlasticStool
      Seat
      Leg
      Leg
      Leg
      Leg
      SeatPoint
  ScaleReferences
    PlayerHeightReference_1.7m
  Lighting
    DirectionalLight
  BlockoutOverviewCamera
```

## Every changed file

The asset inventory includes nine primitive prefabs (stall, NEW sign, and the seven requested placeholders), nine shared materials, one scene, one editor builder, two test files, and their required metadata. The three pre-existing serialized marker/anchor scripts receive stable GUID metadata so a fresh checkout resolves prefab and scene components correctly.

- `Assets/TramChanh.meta`
- `Assets/TramChanh/Art.meta`
- `Assets/TramChanh/Art/Materials.meta`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder.mat`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder.mat.meta`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Ground.mat`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Ground.mat.meta`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Red.mat`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Red.mat.meta`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Reference.mat`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Reference.mat.meta`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_SignTop.mat`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_SignTop.mat.meta`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Stall.mat`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Stall.mat.meta`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Wheel.mat`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Wheel.mat.meta`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Yellow.mat`
- `Assets/TramChanh/Art/Materials/MAT_Placeholder_Yellow.mat.meta`
- `Assets/TramChanh/Art/Materials/MAT_Sign_TramChanh_New.mat`
- `Assets/TramChanh/Art/Materials/MAT_Sign_TramChanh_New.mat.meta`
- `Assets/TramChanh/Prefabs.meta`
- `Assets/TramChanh/Prefabs/CustomerArea.meta`
- `Assets/TramChanh/Prefabs/CustomerArea/PF_PlasticStool.prefab`
- `Assets/TramChanh/Prefabs/CustomerArea/PF_PlasticStool.prefab.meta`
- `Assets/TramChanh/Prefabs/CustomerArea/PF_YellowCrateTable.prefab`
- `Assets/TramChanh/Prefabs/CustomerArea/PF_YellowCrateTable.prefab.meta`
- `Assets/TramChanh/Prefabs/Stall.meta`
- `Assets/TramChanh/Prefabs/Stall/PF_ReadyCounterPoint.prefab`
- `Assets/TramChanh/Prefabs/Stall/PF_ReadyCounterPoint.prefab.meta`
- `Assets/TramChanh/Prefabs/Stall/PF_Sign_TramChanh_New.prefab`
- `Assets/TramChanh/Prefabs/Stall/PF_Sign_TramChanh_New.prefab.meta`
- `Assets/TramChanh/Prefabs/Stall/PF_Stall_TramChanh.prefab`
- `Assets/TramChanh/Prefabs/Stall/PF_Stall_TramChanh.prefab.meta`
- `Assets/TramChanh/Prefabs/Workstations.meta`
- `Assets/TramChanh/Prefabs/Workstations/PF_Grill_Elmich.prefab`
- `Assets/TramChanh/Prefabs/Workstations/PF_Grill_Elmich.prefab.meta`
- `Assets/TramChanh/Prefabs/Workstations/PF_IceBin.prefab`
- `Assets/TramChanh/Prefabs/Workstations/PF_IceBin.prefab.meta`
- `Assets/TramChanh/Prefabs/Workstations/PF_RedTeaRack.prefab`
- `Assets/TramChanh/Prefabs/Workstations/PF_RedTeaRack.prefab.meta`
- `Assets/TramChanh/Prefabs/Workstations/PF_ToppingStation.prefab`
- `Assets/TramChanh/Prefabs/Workstations/PF_ToppingStation.prefab.meta`
- `Assets/TramChanh/Scenes.meta`
- `Assets/TramChanh/Scenes/Gameplay.meta`
- `Assets/TramChanh/Scenes/Gameplay/SCN_Gameplay_Blockout.unity`
- `Assets/TramChanh/Scenes/Gameplay/SCN_Gameplay_Blockout.unity.meta`
- `Assets/TramChanh/Scripts.meta`
- `Assets/TramChanh/Scripts/Core.meta`
- `Assets/TramChanh/Scripts/Core/Provisional.meta`
- `Assets/TramChanh/Scripts/Core/Provisional/PlaceholderAsset.cs.meta`
- `Assets/TramChanh/Scripts/Editor.meta`
- `Assets/TramChanh/Scripts/Editor/Placeholders.meta`
- `Assets/TramChanh/Scripts/Editor/Placeholders/GameplayBlockoutBuilder.cs`
- `Assets/TramChanh/Scripts/Editor/Placeholders/GameplayBlockoutBuilder.cs.meta`
- `Assets/TramChanh/Scripts/Stall.meta`
- `Assets/TramChanh/Scripts/Stall/Anchors.meta`
- `Assets/TramChanh/Scripts/Stall/Anchors/StallAnchor.cs.meta`
- `Assets/TramChanh/Scripts/Stall/Anchors/StallAnchorSet.cs.meta`
- `Assets/TramChanh/Tests.meta`
- `Assets/TramChanh/Tests/EditMode.meta`
- `Assets/TramChanh/Tests/EditMode/Placeholders.meta`
- `Assets/TramChanh/Tests/EditMode/Placeholders/GameplayBlockoutTests.cs`
- `Assets/TramChanh/Tests/EditMode/Placeholders/GameplayBlockoutTests.cs.meta`
- `Assets/TramChanh/Tests/PlayMode.meta`
- `Assets/TramChanh/Tests/PlayMode/Placeholders.meta`
- `Assets/TramChanh/Tests/PlayMode/Placeholders/GameplayBlockoutSmokeTests.cs`
- `Assets/TramChanh/Tests/PlayMode/Placeholders/GameplayBlockoutSmokeTests.cs.meta`
- `QA/Reports/ART-STALL-001-blockout.md`
