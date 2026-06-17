import math
from pathlib import Path

import bpy


ROOT_DIR = Path(__file__).resolve().parents[2]
BLEND_PATH = ROOT_DIR / "blender" / "source" / "can_bin_game_deformed.blend"
FBX_PATH = ROOT_DIR / "blender" / "exports" / "can_bin_game_deformed.fbx"

BLUE = (0.18, 0.44, 0.70, 1.0)
DARK_BLUE = (0.12, 0.28, 0.45, 1.0)
OFF_WHITE = (0.95, 0.94, 0.88, 1.0)
BLACK = (0.02, 0.02, 0.02, 1.0)
WHITE = (0.96, 0.96, 0.92, 1.0)
LABEL_BLUE = (0.12, 0.36, 0.66, 1.0)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    return mat


def assign(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def parent(obj, root):
    obj.parent = root
    return obj


def cube(name, location, scale, mat, root):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    assign(obj, mat)
    return parent(obj, root)


def cylinder(name, location, radius, depth, mat, root, vertices=24, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    assign(obj, mat)
    return parent(obj, root)


def make_ring(name, location, outer_radius, inner_radius, depth, mat, root):
    outer = cylinder(name, location, outer_radius, depth, mat, root, vertices=24, rotation=(math.radians(90), 0.0, 0.0))
    inner_cover = cylinder(
        name + "_InnerBlack",
        (location[0], location[1] - depth * 0.55, location[2]),
        inner_radius,
        depth * 0.55,
        mats["black"],
        root,
        vertices=24,
        rotation=(math.radians(90), 0.0, 0.0),
    )
    return outer, inner_cover


def build_model():
    clear_scene()

    global mats
    mats = {
        "blue": material("Body_Blue", BLUE),
        "dark_blue": material("Dark_Blue", DARK_BLUE),
        "off_white": material("Off_White", OFF_WHITE),
        "black": material("Hole_Black", BLACK),
        "white": material("Line_White", WHITE),
        "label_blue": material("Label_Blue", LABEL_BLUE),
    }

    root = bpy.data.objects.new("CanBin_GameDeformed", None)
    root.empty_display_type = "CUBE"
    root.empty_display_size = 0.35
    root.location = (0.0, 0.0, 0.0)
    bpy.context.collection.objects.link(root)

    # Overall target: height 2.2, width 1.4, depth 1.2.
    cube("Body_Lower", (0.0, 0.0, 0.72), (0.70, 0.60, 0.72), mats["blue"], root)
    cube("Body_Upper", (0.0, -0.01, 1.42), (0.61, 0.54, 0.34), mats["blue"], root)
    cube("Body_FrontPanel", (0.0, -0.615, 1.18), (0.63, 0.018, 0.46), mats["blue"], root)

    cube("TopCover_Base", (0.0, 0.0, 1.80), (0.72, 0.62, 0.17), mats["off_white"], root)
    cube("TopCover_Cap", (0.0, 0.0, 2.04), (0.58, 0.50, 0.20), mats["off_white"], root)
    cube("TopCover_FrontLip", (0.0, -0.625, 1.72), (0.68, 0.035, 0.12), mats["off_white"], root)

    front_y = -0.64
    hole_center = (0.0, front_y, 1.47)
    make_ring(
        "HoleRim",
        hole_center,
        outer_radius=0.40,
        inner_radius=0.34,
        depth=0.025,
        mat=mats["off_white"],
        root=root,
    )

    # Label panel without text objects: white panel, blue inset, and simple can icon.
    cube("FrontLabel_WhitePanel", (0.0, -0.642, 0.75), (0.40, 0.014, 0.29), mats["white"], root)
    cube("FrontLabel_BlueInset", (0.0, -0.660, 0.78), (0.32, 0.010, 0.20), mats["label_blue"], root)
    cylinder(
        "FrontLabel_CanIcon",
        (-0.02, -0.675, 0.80),
        radius=0.055,
        depth=0.24,
        mat=mats["white"],
        root=root,
        vertices=12,
        rotation=(math.radians(80), 0.0, math.radians(-18)),
    )
    cube("FrontLabel_CanSlash", (0.06, -0.681, 0.82), (0.015, 0.006, 0.16), mats["white"], root)
    cube("FrontLabel_TextBar_1", (-0.09, -0.668, 0.58), (0.10, 0.009, 0.018), mats["white"], root)
    cube("FrontLabel_TextBar_2", (0.04, -0.668, 0.58), (0.16, 0.009, 0.018), mats["white"], root)

    for z in (0.27, 0.36):
        cube(f"BottomLine_Front_{z}", (0.0, -0.622, z), (0.67, 0.014, 0.018), mats["white"], root)
        cube(f"BottomLine_Left_{z}", (-0.704, 0.0, z), (0.014, 0.56, 0.018), mats["white"], root)
        cube(f"BottomLine_Right_{z}", (0.704, 0.0, z), (0.014, 0.56, 0.018), mats["white"], root)

    cube("SideHandle_Frame", (0.720, -0.10, 1.12), (0.025, 0.18, 0.09), mats["off_white"], root)
    cube("SideHandle_Inset", (0.744, -0.10, 1.12), (0.015, 0.12, 0.045), mats["black"], root)

    hole_empty = bpy.data.objects.new("HoleCenter", None)
    hole_empty.empty_display_type = "SPHERE"
    hole_empty.empty_display_size = 0.10
    hole_empty.location = hole_center
    bpy.context.collection.objects.link(hole_empty)
    hole_empty.parent = root

    return root


def save_outputs(root):
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    FBX_PATH.parent.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root

    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_space_transform=False,
    )


def main():
    root = build_model()
    save_outputs(root)
    print(f"Saved blend: {BLEND_PATH}")
    print(f"Saved fbx: {FBX_PATH}")


if __name__ == "__main__":
    main()
