# ArtSource

Source art outside Unity ([AP] §70). Binary files go through Git LFS (`.gitattributes`).

```text
ArtSource/
├── References/     real Tram Chanh photos (supplied by the Product Owner)
├── Blender/        *.blend working files, versioned SM_<Asset>_v001.blend …
│   └── Scripts/    reproducible blockout scripts
├── AI_Generated/   raw AI 3D output (never game-ready as-is)
├── Export/         FBX for Unity (SM_<Asset>_FINAL.fbx)
└── Textures/       source textures; branding comes from PO artwork only
```

## ART-STALL-001 — placeholder (phase A) workflow

The stall placeholder exists in two equivalent forms with the same names and the same
GT-001 dimensions. Gameplay never waits for either.

| Where | How | Output |
|---|---|---|
| Unity (default) | Menu **Tram Chanh ▸ Placeholders ▸ Build Stall + New Sign Placeholder** | `Assets/TramChanh/Prefabs/Stall/PF_Stall_TramChanh.prefab`, `PF_Sign_TramChanh_New.prefab`, placeholder materials |
| Blender (artist hand-off) | `blender --background --python Blender/Scripts/stall_blockout.py -- --out Export/SM_Stall_TramChanh_Blockout.fbx` | FBX with `SM_Stall_*` meshes; the script asserts GT-001 before exporting |

Rules that apply to both:

- **Ground truth:** 1.8 m × 0.8 m footprint, counter top 1.0 m, counter → roof 1.2 m, total ~2.2 m.
- **Provisional:** wheel/post/slab/roof sizes, A-frame lean, sign size, and every station anchor
  position. They are blockout values, not measurements. Station positions are edited on the
  anchors in the prefab/scene (`StallAnchor`, *Position Confirmed* off) and are replaced
  from the real stall reference (DEC-011). Rebuilding the Unity placeholder keeps edited anchors.
- **Sign:** only `PF_Sign_TramChanh_New`, a sized placeholder volume with the
  `MAT_Sign_TramChanh_New` material slot. The real artwork is applied later as a texture.
  The old illuminated TRAM CHANH letters are never modelled.
- **Final art swap (phase B):** keep the hierarchy group names and anchor names; remove the
  `PlaceholderAsset` component when the prefab holds final art (the builder then refuses
  to overwrite it).
