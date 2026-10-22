# Character and animal PBR pass 2

Status: materials change only when the owner runs the Unity menu `Tram Chanh > Art > Apply Character And Animal PBR Pass 2`. The applier (`Assets/TramChanh/Scripts/Editor/CharacterPbrApplier.cs`) has NOT been run or compiled: no Unity session was available. "Before" values below are read from the `.mat` files in this branch; "after" values are what the applier will write.

Rule kept: colours stay as approved. The applier never writes `_BaseMap` or `_BaseColor` (it logs an error if either differs after a change). No mesh, rig, animator, prefab or scene is touched.

## What the geometry allows (checked in the files)

| Group | Mesh | UV0 | Tangents | Consequence |
|---|---|---|---|---|
| `PF_Person_01..06`, `PF_Rider_01/02`, all `PF_Friend_*` (variants of `PF_Person_01`) | `M_Person_NN_LOD*.asset` | none on 01, 04, 06; present on 02, 03, 05 | none | a tiling or normal map would sample one texel, or break without tangents. Scalar changes only. The materials are shared between meshes with and without UVs. |
| `PF_Person_07/08` | `SM_Person_MaleCasual/FemaleTankTop.fbx` | none | n/a | scalar only (they use materials embedded in the FBX, not touched) |
| `PF_Street_Cat/Dog/Chicken` | `SM_Street_*.fbx` | palette strip, V span about 0.007 (3 to 4 px of the 512 atlas), U span 0.13 to 0.18 | n/a | UVs only index colours in `T_Animal_Atlas.png`. No fur micro detail is possible, but a map aligned with the atlas can vary smoothness per colour. |
| `PF_Street_Pigeon` | separate flat materials | palette | n/a | scalar only |

URP Lit tiles every primary map (normal, metallic-gloss) with `_BaseMap_ST`; only the detail maps have their own tiling (`_DetailAlbedoMap_ST`). `_DetailNormalMap`, `_DetailNormalMapScale` and `_DetailAlbedoMapScale` exist in the existing `.mat` files, so the shader supports detail normals. The applier contains the code (`_DETAIL_SCALED` with `_DetailAlbedoMapScale` 0, so albedo is unchanged) but it is switched off (`EnableDetailNormals = false`) because no current mesh can use it. URP Lit has no `_MaskMap` (that is HDRP); the mask goes into `_MetallicGlossMap` (R metallic, A smoothness).

## Textures (`Assets/TramChanh/Art/Textures/Characters/`, 512 px, made by `ArtSource/Scripts/generate_character_pbr_v2.py`)

| File | Use | Applied now |
|---|---|---|
| `T_Animal_Atlas_Mask_v2.png` | R 0, G 255, A smoothness per atlas colour, aligned 1:1 with `T_Animal_Atlas.png` | yes, `MAT_Animal_Atlas` `_MetallicGlossMap` |
| `T_Cloth_Weave_Normal_v2`, `T_Skin_Micro_Normal_v2`, `T_Fur_Micro_Normal_v2`, `T_Hair_Strand_Normal_v2`, `T_Leather_Grain_Normal_v2`, `T_Rubber_Sole_Normal_v2` | seamless detail normals | no material uses them yet (see above); the applier still sets their import type to Normal map so they are ready |

The atlas mask is a colour heuristic, not a region paint: dark neutral colours (eyes, nose leather, pads) get 0.75, pink 0.40, strongly saturated yellow (beak) 0.38, strong red (comb) 0.40, everything else (fur, feathers) 0.20. `MAT_Animal_Atlas` gets `_Smoothness` 1 so the map value is the final smoothness. Which atlas block is which body part was not checked in game, so a dark fur patch may come out glossier than intended.

## Per-material change

Friends: the 54 slot materials `PF_Friend_NN_*_K_Part_*` all have `_Smoothness` 0.1 before. After, by slot: 0 Shirt 0.12, 1 Skin 0.28, 2 Pants 0.10, 3 Shoes 0.32, 4 Soles 0.12, 5 Hair 0.38. `_Metallic` stays 0.

| Material | Class | _Smoothness before | _Smoothness after | _Metallic before -> after |
|---|---|---|---|---|
| `F_Beads` | plastic beads (unchanged) | 0.6 | 0.6 | 0 -> 0 |
| `F_Black` | cotton cloth | 0.15 | 0.12 | 0 -> 0 |
| `F_CamoCap` | cap fabric | 0.05 | 0.12 | 0 -> 0 |
| `F_Cream` | knit cloth | 0.05 | 0.1 | 0 -> 0 |
| `F_Fleece` | fleece cloth | 0.05 | 0.08 | 0 -> 0 |
| `F_GlassFrame` | acetate frame (unchanged) | 0.8 | 0.8 | 0 -> 0 |
| `F_Gold` | metal (unchanged) | 0.7 | 0.7 | 0.9 -> 0.9 |
| `F_Gray` | sweat cloth | 0.1 | 0.1 | 0 -> 0 |
| `F_HairBlack` | hair | 0.3 | 0.38 | 0 -> 0 |
| `F_HairBlondTip` | hair | 0.3 | 0.4 | 0 -> 0 |
| `F_HairBrown` | hair | 0.3 | 0.38 | 0 -> 0 |
| `F_HairCopper` | hair | 0.3 | 0.38 | 0 -> 0 |
| `F_Mouth` | lips | 0.2 | 0.4 | 0 -> 0 |
| `F_RedVarsity` | wool/nylon jacket | 0.1 | 0.18 | 0 -> 0 |
| `F_SweatBeige` | fleece cloth | 0.1 | 0.08 | 0 -> 0 |
| `F_White` | cotton cloth | 0.2 | 0.14 | 0 -> 0 |
| `MAT_Animal_Atlas` | atlas fur/feather + palette mask | 0.5 | 1 | 0 -> 0 |
| `MAT_Animal_Cat_Ears` | inner ear skin | 0.5 | 0.3 | 0 -> 0 |
| `MAT_Animal_Cat_Main` | fur | 0.5 | 0.2 | 0 -> 0 |
| `MAT_Animal_Cat_Secondary` | fur | 0.5 | 0.2 | 0 -> 0 |
| `MAT_Animal_Chicken_Beak` | keratin | 0.5 | 0.45 | 0 -> 0 |
| `MAT_Animal_Chicken_Main` | feather | 0.5 | 0.3 | 0 -> 0 |
| `MAT_Animal_Chicken_Red` | comb / wattle | 0.5 | 0.45 | 0 -> 0 |
| `MAT_Animal_Dog` | fur | 0.5 | 0.2 | 0 -> 0 |
| `MAT_Animal_Eye_Black` | eye | 0.5 | 0.9 | 0 -> 0 |
| `MAT_Animal_Eye_White` | eye white | 0.5 | 0.6 | 0 -> 0 |
| `MAT_Animal_Pigeon_Main` | feather | 0.5 | 0.3 | 0 -> 0 |
| `MAT_Animal_Pigeon_Secondary` | feather | 0.5 | 0.3 | 0 -> 0 |
| `MAT_DogGrin_Dark` | nose / eye | 0.2 | 0.7 | 0 -> 0 |
| `MAT_DogGrin_Fur` | fur | 0.1 | 0.12 | 0 -> 0 |
| `MAT_DogGrin_Hair` | fur / hair | 0.25 | 0.2 | 0 -> 0 |
| `MAT_DogGrin_Silver` | metal (unchanged) | 0.7 | 0.7 | 0.9 -> 0.9 |
| `MAT_DogGrin_Tee` | cotton cloth | 0.1 | 0.12 | 0 -> 0 |
| `MAT_DogGrin_Teeth` | enamel (unchanged) | 0.5 | 0.5 | 0 -> 0 |
| `MAT_Person_Clothing_A` | cotton cloth | 0.15 | 0.12 | 0 -> 0 |
| `MAT_Person_Clothing_B` | cotton cloth | 0.25 | 0.12 | 0 -> 0 |
| `MAT_Person_Eyes_Dark` | eye | 0.5 | 0.9 | 0 -> 0 |
| `MAT_Person_Hair_Accessories` | cotton cloth | 0.15 | 0.12 | 0 -> 0 |
| `MAT_Person_Hair_Black` | hair | 0.25 | 0.38 | 0 -> 0 |
| `MAT_Person_Hair_Caramel` | hair | 0.25 | 0.38 | 0 -> 0 |
| `MAT_Person_Hair_Chestnut` | hair | 0.25 | 0.38 | 0 -> 0 |
| `MAT_Person_Hair_DarkBrown` | hair | 0.25 | 0.38 | 0 -> 0 |
| `MAT_Person_Hair_DarkIndigo` | hair | 0.25 | 0.38 | 0 -> 0 |
| `MAT_Person_Hat_Orange` | cap fabric | 0.2 | 0.15 | 0 -> 0 |
| `MAT_Person_Hoodie_Teal` | fleece cloth | 0.15 | 0.08 | 0 -> 0 |
| `MAT_Person_Pants_Charcoal` | twill / denim | 0.15 | 0.1 | 0 -> 0 |
| `MAT_Person_Pants_Navy` | twill / denim | 0.15 | 0.1 | 0 -> 0 |
| `MAT_Person_Pants_Olive` | twill / denim | 0.15 | 0.1 | 0 -> 0 |
| `MAT_Person_Shirt_Coral` | cotton cloth | 0.15 | 0.12 | 0 -> 0 |
| `MAT_Person_Shirt_White` | cotton cloth | 0.18 | 0.14 | 0 -> 0 |
| `MAT_Person_Shoes_Black` | leather | 0.2 | 0.38 | 0 -> 0 |
| `MAT_Person_Shoes_Brown` | leather | 0.25 | 0.38 | 0 -> 0 |
| `MAT_Person_Shoes_Soles` | rubber sole | 0.15 | 0.12 | 0 -> 0 |
| `MAT_Person_Shoes_Tan` | leather | 0.2 | 0.35 | 0 -> 0 |
| `MAT_Person_Shoes_White` | synthetic upper | 0.2 | 0.3 | 0 -> 0 |
| `MAT_Person_Skin` | skin | 0.15 | 0.28 | 0 -> 0 |
| `MAT_Person_Skin_Bronze` | skin | 0.2 | 0.28 | 0 -> 0 |
| `MAT_Person_Skin_Fair` | skin | 0.2 | 0.28 | 0 -> 0 |
| `MAT_Person_Skin_Golden` | skin | 0.2 | 0.28 | 0 -> 0 |
| `MAT_Person_Skin_Light` | skin | 0.2 | 0.28 | 0 -> 0 |
| `MAT_Person_Skin_Olive` | skin | 0.2 | 0.28 | 0 -> 0 |
| `MAT_Person_Skin_Tan` | skin | 0.2 | 0.28 | 0 -> 0 |
| `MAT_Person_Top_Yellow` | cotton cloth | 0.15 | 0.12 | 0 -> 0 |
| `MAT_Person_Vest_Yellow` | hi-vis polyester | 0.25 | 0.3 | 0 -> 0 |

Why these numbers: skin 0.28 (a little sheen; no normal detail breaks it up, so it stays under 0.35 to avoid plastic); cloth 0.08 to 0.14 (matte, fleece lowest); jackets and hi-vis 0.18 to 0.30; leather shoes 0.35 to 0.38; rubber soles 0.12; hair 0.38 (a broad soft highlight, the nearest a scalar gets to an anisotropic look); eyes 0.9; keratin and comb 0.45; fur 0.12 to 0.20. Metals (`F_Gold`, `MAT_DogGrin_Silver`), beads, glasses frame and teeth are in scope but unchanged.

## Not delivered, and why

- Skin forehead/nose gloss mask, fabric weave, hair strand highlight, fur micro normal: all need UV0 on the character meshes (and tangents for normal maps). Meshes were off limits. Once `M_Person_*` has UVs and tangents, set `EnableDetailNormals` to true and call `ApplyDetailNormal` per class with the tiling you want.
- Per-species tint variation: cat, dog and chicken prefabs share one atlas slot, so there are no separate instances to tint.
- Not run in Unity: no compile, no menu run, no screenshots.
