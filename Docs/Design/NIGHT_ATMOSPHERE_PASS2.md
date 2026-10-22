# Night atmosphere pass 2

Tool: `Assets/TramChanh/Scripts/Editor/NightAtmospherePass2.cs`
Menus: `Tram Chanh > Art > Apply Night Atmosphere Pass 2` and `Tram Chanh > Art > Remove Night Atmosphere Pass 2`.

**Status: the tool has NOT been compiled and NOT been run.** It was written without Unity. All costs below are estimates from the data in the repo (QA report `QA/Screenshots/Capture_20261015_1519/street_performance_report.txt`, scene and prefab YAML). Nothing was measured. The first run in Unity is a test: read the Console log (every change is logged with the prefix `[NightAtmospherePass2]`).

## 0. Facts found in the repo that shaped the design

| Finding | Consequence |
|---|---|
| `SCN_TramChanh_Main` holds only Moonlight, `RP_Stall`, `PostProcessVolume`, the player and the bootstrap. The street and the stall are instantiated at run time from `PF_TramChanh_StreetExtended` (bootstrap field `_environmentPrefab`, guid `3c650c60...`). | Street, stall and lamp lights cannot be tuned on scene objects. The tool edits the lights inside `PF_TramChanh_StreetExtended.prefab` (as overrides on the nested lamp and bulb prefabs). The probes need a temporary copy of the street to bake against. |
| Real-time lights inside the street prefab: 21 x `LuminaireLight` (spot, from `PF_StreetLamp`, 3.6, range 14, shadows off), `WarmStallBulb` (point 3.8, range 6, soft shadows), `Streetlight_Center` (point 3.2, range 22, no shadows), `SkyFill` (directional 1.5, cool, no shadows). Inside `PF_Stall_TramChanh`: 4 x `PF_EdisonBulb_n/PointLight` (point 2.8, range 3.5, **soft shadows**). Scene: `Moonlight` (directional 0.6, soft shadows). | Four soft-shadow point lights next to the stall are the most expensive item (each can need up to 6 shadow faces). They are switched to no shadows. |
| `MAT_Stall_Bulb_Filament.mat` has `m_ValidKeywords: []`: the `_EMISSION` keyword is off, so its `_EmissionColor (3.5, 2, 0.6)` is probably not rendered today. `MAT_Stall_Bulb_Glass.mat` has emission black. | The tool enables `_EMISSION`. This is what makes the bulbs bloom, at no per-frame cost. |
| `MAT_Street_StreetLamp.mat` is used by the whole lamp mesh (pole and head are one mesh `SM_StreetLamp.obj`). | Its emission is NOT touched (the pole would glow). A lamp-head emissive needs a second sub-mesh and material: art task, not done here. Same for `MAT_Accel_WarmBulb` (Accel scene only). |
| Scene fog today: `m_FogMode: 2` (Exponential), density 0.024, colour (0.018, 0.031, 0.067). That is very thick: 30 % visible at 50 m, 9 % at 100 m, 0.3 % at 250 m. | Fog is changed to exponential squared with a lower density (section 3). |
| `VP_TramChanh_Night.asset` as committed contains Tonemapping (ACES), Bloom, Vignette, ColorAdjustments, WhiteBalance and **two empty component slots**. The DoF and film grain named in commit b726c1b are not in the file. | v2 removes the empty slots and adds a light film grain. DoF is not added (not requested, costs a blur pass). |
| `PF_TramChanh_StreetExtended` and `StallHighFidelityBuilder` regenerate these prefabs. | If a builder is run again, run Apply again. Durable fix: move the numbers into the builders (not done, those files are not owned by this task). |
| The URP pipeline asset is not tracked. | Shadow resolution, additional-light shadow switches, probe blending and SSAO are not touched and not known. All estimates assume the defaults. |

## 1. Lights (no light is added)

Colour comes from a black-body approximation (`KelvinToColor`), because `Light.useColorTemperature` needs a project setting that is not changed here.

| Light | Before | After | Reason |
|---|---|---|---|
| Street lamps x21 (sorted by x, pattern by index) | warm white (1, 0.84, 0.60), 3.6, range 14, spot 78 deg | pattern 2700 / 3000 / 2700 / 3500 K = (1, 0.65, 0.34) / (1, 0.70, 0.43) / (1, 0.65, 0.34) / (1, 0.76, 0.55); intensity 4.0 / 3.8 / 4.0 / 3.6; range 13; inner spot angle 45 deg; shadows none | 2 of 4 sodium-like, 1 warm LED, 1 cooler LED: breaks the "all lamps identical" look. Intensity is raised a little because range 14 to 13 trims the soft edge of the pool (URP window factor). Inner spot angle 45 gives a soft pool edge instead of a hard disc. |
| `WarmStallBulb` (stall key light, the only light that keeps a shadow) | (1, 0.82, 0.50), 3.8, range 6, soft | 2900 K, 4.0, range 6.5, soft | The key light is the one shadow worth paying for: it grounds the counter and cups. |
| `Streetlight_Center` | (1, 0.86, 0.65), 3.2, range 22 | 3200 K, 2.8, range 18 | Range 22 reached far past the lamps and washed out contrast. |
| `PF_EdisonBulb_0` and `_3` (outer) | (1, 0.82, 0.55), 2.8, range 3.5, soft | 2700 K, 4.0, range 4.0, shadows none | Two lights do the work of four, so each is stronger. |
| `PF_EdisonBulb_1` and `_2` (inner) | real-time point light | light component disabled (mesh and emissive filament stay) | Brings the real-time lights near x = 0 from 8 to 6 (constant `DisableInnerEdisonLights`). |
| `SkyFill` | directional 1.5 | untouched (logged) | Bootstrap treats the first directional light as the sun; not risked. |
| `Moonlight` (scene) | soft shadows | shadows none (constant `MoonShadows`) | Only directional shadow pass. Set to `Soft` in the constant to keep it. Colour, intensity and angle are untouched (bootstrap drives them). |

Bulb materials (both existing, edited in place, original file copied to `Library/TramChanh/NightAtmospherePass2/Backup`):

| Material | Before | After |
|---|---|---|
| `MAT_Stall_Bulb_Filament` | no `_EMISSION` keyword, emission (3.5, 2, 0.6) | keyword on, 2700 K x 6.0 = (6.0, 3.9, 2.1), flag RealtimeEmissive. Value above 1 so it passes the new bloom threshold 0.9. |
| `MAT_Stall_Bulb_Glass` (transparent) | no emission | keyword on, 2700 K x 0.6, subtle glow in the glass |

## 2. Fake glow (instead of more lights)

- One root child `NightAtmosphere/FakeGlow`, one object per street lamp (21), placed at the lamp light position.
- Mesh: three crossed discs (two vertical, one horizontal), 4 rings, alpha falloff 0.90 / 0.45 / 0.12 / 0 at radius 0 / 0.2 / 0.55 / 1.1 m, about 180 triangles. Three shared meshes (2700, 3000, 3500 K), colour in vertex colour. Crossed discs replace a billboard so no runtime script is needed.
- One shared material `MAT_NightGlow_Additive` (`Universal Render Pipeline/Particles/Unlit`, transparent, additive `SrcAlpha One`, ZWrite off, two-sided, `_BaseColor` 1.5, GPU instancing on). Falls back to `Legacy Shaders/Particles/Additive` if the URP shader is not found.
- Pooling by distance without a script: each glow has a one-level `LODGroup` that culls it below 1.5 % of screen height (about 125 m at 60 deg vertical field of view). Real pooling (move 8 quads to the nearest lamps) would need a runtime script, which this task may not add.
- No shadows, no light probes, no reflection probes, no motion vectors.

## 3. Fog, ambient, skybox

| Setting | Before | After | Reason |
|---|---|---|---|
| Fog mode | Exponential | Exponential squared | Clear near the player, fades late: the street keeps its depth and the 500 m still reads. |
| Fog density | 0.024 | 0.0055 | Visibility `exp(-(d x 0.0055)^2)`: 50 m 93 %, 100 m 74 %, 150 m 51 %, 250 m 15 %, 400 m 1 %. |
| Fog colour | (0.018, 0.031, 0.067) | (0.055, 0.070, 0.100) cool blue-grey | Close to the horizon colour of the sky material so far buildings melt into the sky. |
| Ambient | Skybox mode, sky (0.24, 0.30, 0.43) unused | Trilight: sky (0.19, 0.24, 0.36), equator (0.13, 0.12, 0.12), ground (0.05, 0.045, 0.04); `ambientIntensity` untouched (bootstrap scales it) | Slightly darker sky fill for more contrast between lamp pools and shade; the equator is a little warm to suggest bounce from lamps. Trilight also avoids a skybox SH update. |
| `MAT_Skybox_Night` | thickness 0.85, exposure 0.9, tint (0.12, 0.16, 0.28), ground (0.04, 0.05, 0.08) | thickness 0.6, exposure 0.7, tint (0.10, 0.14, 0.24), ground (0.035, 0.045, 0.07) | Less blue scattering, darker sky, so lamps and glow stand out. |

## 4. Reflection probes (wet road)

`RP_Stall` today: Realtime, refresh On Awake, 128 px, box 40 x 12 x 40 at (-1.5, 3, 2.7), box projection on, box bottom at y = -3 (below the road).

| Probe | Position | Box | Settings |
|---|---|---|---|
| `RP_Stall` (existing, converted) | unchanged | 44 x 12 x 40, floor moved to y = -0.15 (road top) | Custom, baked by the tool into `RP_Night_Stall.exr`, 128 px, HDR, box projection, importance 2, blend 3, dynamic objects included in the bake |
| `RP_Night_West` (new) | (-91, 3, 6) | 142 x 12 x 40, covers x -162 to -20 | same, importance 1, blend 5 |
| `RP_Night_East` (new) | (91, 3, 6) | 142 x 12 x 40, covers x 20 to 162 | same |

- Road is z 3.6 to 12.6 and the probes sit at z = 6 over the road. Beyond |x| = 162 the scene falls back to the default skybox reflection (so the far ends are not covered).
- The tool bakes with `Lightmapping.BakeReflectionProbe(probe, path)` while a temporary copy of `PF_TramChanh_StreetExtended` is in the scene, then destroys the copy. Mode Custom (not Baked) because Custom can include non-static objects. If the bake call fails or returns false, the probe falls back to Realtime / On Awake / 64 px and the Console says so.
- Cost: baked, so zero per frame apart from the shader blending of up to 2 probes and the box-projection math on the road and sidewalk materials (a few ALU per pixel; URP "Reflection Probe Blending" must be on in the pipeline asset, which cannot be verified here). Three EXR files of 128 px cube HDR, about 0.4 to 0.8 MB each (the repo `.gitattributes` already sends `*.exr` through LFS). Old realtime cost removed: one 128 px six-face render at scene start.
- Limitation: the baked picture shows the street at bake time; moving objects (cars, people) are not in it, and the bake does not include the closed-shop or rain events. That is the usual baked-probe trade.

## 5. Volume profile `VP_TramChanh_Night_v2.asset`

Cloned with `AssetDatabase.CopyAsset` from `VP_TramChanh_Night.asset` (old file untouched). The scene Volume `PostProcessVolume` is switched in an undoable step and the old profile guid is stored for the Remove menu.

| Effect | Old | New | Why |
|---|---|---|---|
| Tonemapping | ACES | unchanged | |
| White balance | +6 / -2 | unchanged | Warm lamps are already helped by split toning. |
| Color adjustments | exposure 0.15, contrast 14, saturation 8 | exposure 0.10, contrast 16, saturation 6 | Lamps and bloom add light, so global exposure goes down a little; more contrast keeps blacks deep; less saturation keeps the fog from turning candy blue. |
| Bloom | threshold 1.0, intensity 0.55, scatter 0.7, clamp 65472, tint white | threshold 0.9, intensity 0.70, scatter 0.75, clamp 30, tint (1, 0.92, 0.82) | Threshold 0.9 so the emissive filament (>= 2) and the glow centres bloom while a lit road (about 0.2 to 0.5) does not. Clamp 30 stops single hot pixels on the wet road from flaring. Warm tint matches the lamps. High quality filtering and iterations unchanged, so cost is the same. |
| Chromatic aberration | absent | present, intensity 0, active off | Explicit off. |
| Split toning | absent | shadows (0.40, 0.50, 0.66), highlights (0.65, 0.54, 0.38), balance -10 | URP applies it as a soft-light blend where grey 0.5 is neutral: red below 0.5 and blue above it cools the shadows; the reverse warms the highlights. Balance -10 gives the cool tint a little more of the tonal range because a night image is mostly shadow. |
| Lift / gamma / gain | absent | lift (0.97, 0.985, 1.04, -0.01), gamma neutral, gain (1.03, 1.00, 0.96, 0) | Small: lift cools and deepens the toe (-0.01 offset), gain warms the top end. Kept small because split toning already does the main job and the two stack. |
| Film grain | slot empty | Thin1, intensity 0.12, response 0.8 | Hides banding in the dark gradient and fog. |
| Vignette | 0.28 | unchanged | |
| Depth of field | not in file | not added | Extra blur pass, not requested. |

## 6. Cost estimate (not measured)

Baseline from the QA report: x = -240: 587 draw calls (the task text says 540; the committed report shows 587 for that capture), x = -120: 488, x = 0: 268 with 8 lights within 30 m, budget 626.

| Item | Real-time lights | Draw calls | Overdraw / other |
|---|---|---|---|
| Light retune | 0 added; x = 0 goes from 8 to 6 | 0 | none |
| Shadows | 4 Edison point-light shadow maps removed (up to 24 face renders), Moonlight cascade shadow pass removed | fewer shadow-caster draws; the saving is probably large at x = 0 but unmeasured | |
| Emissive bulbs | 0 | 0 | one more bloom source; same bloom cost |
| Fake glow | 0 | at most +1 per visible mesh/material batch. With LOD culling at about 125 m and lamps every 24 m, 5 to 11 glows are inside the frustum: estimate +5 to +11 draw calls (SRP Batcher and instancing may merge most). Worst end (x = -240) 587 + 11 = 598, under 626. | Each glow covers about 4 m2 at most. At 10 m it is about 3 % of a 1080p frame, at 25 m about 0.5 %. Sum of the three nearest about 5 % of the screen at about 2 layers, so average overdraw rises by roughly 0.1. Alpha-blended, so it is the main per-pixel cost of this pass. About 3.8k triangles in total, 21 renderers. |
| Probes | 0 | 0 | baked; blending of up to 2 probes on the road material |
| Fog | 0 | 0 | exp2 fog is one `exp2` per pixel |
| Volume v2 | 0 | 0 | same stack as before plus split toning, LGG, grain: they are merged into the uber post shader, so about +0.05 to +0.15 ms on a desktop GPU (estimate) |

## 7. Undo and revert

- Scene changes (root, fog, ambient, Moonlight, probe, Volume) are recorded in the Unity Undo stack. Prefab, material and generated-asset changes are file writes and are not undoable with Ctrl+Z.
- `Remove Night Atmosphere Pass 2`: puts the Volume back on the old profile, deletes the `NightAtmosphere` root, deletes `Assets/TramChanh/Art/Lighting/NightAtmosphere` (glow meshes, glow material, EXR files) and `VP_TramChanh_Night_v2.asset`, copies the saved originals of the street prefab, two bulb materials and the skybox material back, and restores fog, ambient, Moonlight shadows and `RP_Stall` from `Library/TramChanh/NightAtmospherePass2/state.json`. Save the scene afterwards.
- Apply twice is safe: values are set again, the root and the generated folder are rebuilt, the first backup (the true original) is kept.
- If the Library folder is deleted between Apply and Remove, the file restore and scene restore are lost: use `git checkout` on the prefab and materials then (lead action).

## 8. Screenshots for the lead

Run `Tram Chanh > QA > Capture Standard Views` in Play Mode **before** Apply and again **after** Apply (and after saving the scene). Keep the two folders side by side.

Checklist (use the standard views, plus the extra ones marked +):
1. Stall front, counter level, with the 4 bulbs in frame: filament bloom and glass glow; the inner two bulbs no longer cast light (check cup and counter shadows come from the key light only).
2. Stall from the street at eye height: key-light shadow still present, no flat look.
3. Street at x = 0, looking east and looking west: lamp pools (warm / cool mix), fake glow around lamp heads, no visible disc edges.
4. Street at x = -120 and x = +120: lamp rhythm, glow culling (no popping in the middle distance; if it pops, lower `GlowCullScreenHeight`).
5. Street at x = -240 and x = +240: far buildings read through the fog (target: the end wall is still a visible silhouette). If too foggy, lower `FogDensity` to 0.004.
6. + Wet road looking along the street from x = 0: reflections of lamps in the road and stretching correctly (box projection floor on the road). Compare against the same view before.
7. + Wet road at x = -100 and x = +100 (inside the new probes): reflection must not jump when walking from the stall probe into a street probe (watch the blend zone at |x| = 20).
8. + Sky and horizon at the street ends: fog colour must match the horizon (no visible band).
9. + One Play Mode night with rain or power-cut event (NightLook): lamps dim by the bootstrap factor, glow does not (glow is static; if this is visible tell the lead, a runtime script would be needed to scale it).
10. Stats window numbers (draw calls, SetPass, triangles) at -240, -120, 0, +120, +240 against `street_performance_report.txt`, and `Tram Chanh > QA` budget report if available.

## 9. Not verified

Compilation, every API call in the tool, the bake result, whether `Universal Render Pipeline/Particles/Unlit` accepts the property values set from code, the real draw-call and light numbers, the look. The lamp positions assume the instantiated street sits at the world origin (the tool logs the prefab root position).
