# Art pipeline: the rules that already cost us time

## Units and pivots
- 1 Unity unit = 1 metre. A tea bag is about 0.22 m tall, a stool seat is 0.30 m, the counter top is at y = 1.00 m, a person is 1.65 to 1.70 m.
- Put the pivot at the bottom centre of an object that stands on something. The tea bag pouch meshes start at y = 0; an old placement point at -0.18 made every placed bag float 0.18 m. A hanging object (lamp) is the exception: pivot at the hook.
- A prefab's placement point must be the point that touches the surface.

## OBJ files are mirrored on X
- Unity mirrors an OBJ on the X axis when it imports it. A hole that you cut at +0.6 m in the file is at -0.6 m in the scene.
- The stall counter is authored with the cake wing at -x and the topping hole at +x as seen from the seller; the stall prefab flips the counter mesh (scale x = -1) to put it back. If you rebuild with `StallHighFidelityBuilder`, that flip is already in the script.
- `Automation/prefab_bounds.py` applies the same mirror, so its numbers match what you see in the game.
- Prefer FBX or GLB for anything with left/right asymmetry. If you must use OBJ, check the result in the game, not only in your modelling tool.

## Stall layout (owner's reference photo, seller looking out)
- Left: cake grill and cake tools. Middle: ice bin. Right: topping tray (long side runs front to back).
- The red tea crate stands on a plastic stool behind the counter, in a row with the decorative crates.
- Drop-in objects sit in the counter holes with the rim level with the counter top; do not let a bin pass through the counter slab.

## Budgets and imports
- Triangles: whole street at the player's eye 400,000 or fewer; single prop 50,000 or fewer.
- Light budget: at most 8 real-time lights near the player; more become baked or emissive.
- Colliders are boxes. No mesh colliders on props.
- Textures: reuse atlases; no new 2048 px texture per module. Palette atlases use point filtering and no mipmaps.
- Everything binary goes through Git LFS; small generated sounds are the one exception (`Resources/Audio/.gitattributes`).

## Check list before you say a model is ready
1. Pivot at the bottom, scale 1 = 1 m, no stray primitives left from a blockout (a hidden `TeaLiquid` cube showed under every closed bag).
2. Placed in the real scene next to the neighbours: nothing floating, nothing buried, nothing overlapping (`prefab_bounds.py --overlaps --floating`).
3. Screenshot taken in `SCN_TramChanh_Main` with the shared capture tool, from the player's eye height and from one overview angle.
4. Licence row added, Credits entry added if CC-BY.
5. Audit and preflight pass; Unity tests pass or the failure text is pasted.

## Buildings are solid cubes (owner decision)
- Every building (towers, shopfronts, boundary and backdrop buildings, alley walls, world-edge gates) is made of solid closed cuboids: 8 corners, 6 outward sides, nothing cut into the volume.
- Doors, windows, shutters, signs and shop interiors are painted in the texture, never modelled as recesses or openings. A player must never see "inside" a building.
- Awnings, balconies and signs may be separate closed cuboids that touch the building; they must not open a hole in it.
- Check with `python3 Automation/building_cube_check.py` (needs `git lfs pull`). The check is part of the pre-push list.
