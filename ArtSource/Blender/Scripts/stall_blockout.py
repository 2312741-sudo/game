"""ART-STALL-001 phase A — Tram Chanh stall blockout for Blender 4.x.

Builds the modular stall placeholder meshes with the final names from
ASSET_INTEGRATION.md §5.1 and exports one FBX for Unity.

Ground truth (GT-001) — never change:
    width 1.8 m, depth 0.8 m, counter top 1.0 m, counter -> roof 1.2 m, total ~2.2 m

Everything else in PROVISIONAL is a blockout value, NOT a real measurement.
Replace it from the real stall reference (DEC-011) in phase B.

The old illuminated TRAM CHANH letters are never modelled (GT-002). The new sign is
a separate asset (ART-BRAND-001); this script only creates a placeholder volume
for it when --with-sign-placeholder is passed.

Usage:
    blender --background --python stall_blockout.py -- \
        --out ../../Export/SM_Stall_TramChanh_Blockout.fbx [--with-sign-placeholder]

Axes: Blender +Z up, -Y = front (customer / Lobby side). The FBX export converts to
Unity (+Y up, +Z front) with axis_forward='-Z', axis_up='Y'.
"""

import argparse
import sys

import bpy
import mathutils

# ---- Ground truth (GT-001) -------------------------------------------------------
WIDTH = 1.8
DEPTH = 0.8
COUNTER_TOP = 1.0
COUNTER_TO_ROOF = 1.2
TOTAL_HEIGHT = COUNTER_TOP + COUNTER_TO_ROOF

# ---- Provisional blockout values (NOT real-world measurements) -------------------
PROVISIONAL = {
    "wheel_diameter": 0.10,
    "wheel_width": 0.04,
    "wheel_inset": 0.08,
    "counter_slab_thickness": 0.04,
    "roof_thickness": 0.04,
    "post_size": 0.05,
    "sign_size": (1.70, 0.12, 0.30),  # x, depth (y), height (z) — provisional
}


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True, help="FBX output path")
    parser.add_argument("--with-sign-placeholder", action="store_true")
    return parser.parse_args(argv)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def box(name, center, size, parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if parent is not None:
        obj.parent = parent
    return obj


def empty(name, location=(0.0, 0.0, 0.0), parent=None):
    obj = bpy.data.objects.new(name, None)
    obj.location = location
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    return obj


def build(with_sign_placeholder):
    p = PROVISIONAL
    root = empty("PF_Stall_TramChanh")

    structure_bottom = p["wheel_diameter"]
    slab_bottom = COUNTER_TOP - p["counter_slab_thickness"]
    roof_bottom = TOTAL_HEIGHT - p["roof_thickness"]

    box("SM_Stall_Base", (0, 0, (structure_bottom + slab_bottom) / 2),
        (WIDTH, DEPTH, slab_bottom - structure_bottom), root)
    box("SM_Stall_Counter", (0, 0, COUNTER_TOP - p["counter_slab_thickness"] / 2),
        (WIDTH, DEPTH, p["counter_slab_thickness"]), root)

    post_h = roof_bottom - COUNTER_TOP
    px = WIDTH / 2 - p["post_size"] / 2
    py = DEPTH / 2 - p["post_size"] / 2
    i = 0
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(f"SM_Stall_Frame_Post{i}", (sx * px, sy * py, COUNTER_TOP + post_h / 2),
                (p["post_size"], p["post_size"], post_h), root)
            i += 1

    box("SM_Stall_Roof", (0, 0, TOTAL_HEIGHT - p["roof_thickness"] / 2),
        (WIDTH, DEPTH, p["roof_thickness"]), root)

    wx = WIDTH / 2 - p["wheel_inset"]
    wy = DEPTH / 2 - p["wheel_inset"]
    i = 0
    for sx in (-1, 1):
        for sy in (-1, 1):
            bpy.ops.mesh.primitive_cylinder_add(
                radius=p["wheel_diameter"] / 2, depth=p["wheel_width"],
                location=(sx * wx, sy * wy, p["wheel_diameter"] / 2), rotation=(0, 1.5707963, 0))
            wheel = bpy.context.active_object
            wheel.name = f"SM_Stall_CasterWheel{i}"
            wheel.data.name = wheel.name
            wheel.parent = root
            i += 1

    if with_sign_placeholder:
        sx, sd, sz = p["sign_size"]
        # Rear-centre pivot; the sign sits inside the footprint, front face flush with the front edge.
        sign = empty("PF_Sign_TramChanh_New", (0, -(DEPTH / 2 - sd), roof_bottom - 0.01 - sz / 2), root)
        # Parenting without an inverse matrix: the child location is local to the sign empty.
        box("SM_Sign_TramChanh_New", (0, -sd / 2, 0), (sx, sd, sz)).parent = sign

    return root


def verify():
    """Fail loudly if the blockout violates GT-001."""
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    xs, ys, zs = [], [], []
    for o in meshes:
        for corner in o.bound_box:
            world = o.matrix_world @ mathutils.Vector(corner)
            xs.append(world.x)
            ys.append(world.y)
            zs.append(world.z)
    width, depth, height = max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)
    assert abs(width - WIDTH) <= 0.02, f"width {width:.3f}"
    assert abs(depth - DEPTH) <= 0.02, f"depth {depth:.3f}"
    assert abs(height - TOTAL_HEIGHT) <= 0.05, f"height {height:.3f}"
    counter = bpy.data.objects["SM_Stall_Counter"]
    counter_top = max((counter.matrix_world @ mathutils.Vector(c)).z for c in counter.bound_box)
    assert abs(counter_top - COUNTER_TOP) <= 0.01, f"counter top {counter_top:.3f}"
    print(f"[ART-STALL-001] GT-001 OK: {width:.3f} x {depth:.3f} x {height:.3f} m, counter {counter_top:.3f} m")


def export(path):
    bpy.ops.export_scene.fbx(
        filepath=path,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        use_space_transform=True,
        bake_space_transform=True,
        object_types={"MESH", "EMPTY"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
    )


def main():
    args = parse_args()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    clear_scene()
    build(args.with_sign_placeholder)
    verify()
    export(args.out)
    print(f"[ART-STALL-001] exported {args.out}")


if __name__ == "__main__":
    main()
