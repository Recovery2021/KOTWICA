"""
KOTWICA - modular corridor kit generator (art direction test #1)

What it does:
  1. Creates the shared material palette (same names as in Unity).
  2. Builds 12 modular corridor pieces on a 1 m grid (bevelled edges, box-mapped UVs).
  3. Exports every piece as its own FBX into the Unity project.
  4. Saves this scene as Blender/kit_corridor.blend.

How to run (Blender 3.2 or newer):
  Scripting tab -> Open -> this file -> Run Script (Alt+P).
  Then in Unity: menu Kotwica -> Zbuduj scenę testu klimatu.

All geometry is written in UNITY coordinates (Y up, Z forward, metres)
and converted to Blender axes at the end, so numbers match the Unity scene 1:1.
"""

import math
import os

import bmesh
import bpy
from mathutils import Euler, Vector

# --- Settings ---------------------------------------------------------------

PROJECT_DIR = r"E:\Unity\KOTWICA"                      # Unity project root
EXPORT_SUBDIR = os.path.join("Assets", "_Project", "Art", "Models", "Kit_Corridor")
BLEND_PATH = os.path.join(PROJECT_DIR, "Blender", "kit_corridor.blend")
COLLECTION_NAME = "KIT_Corridor"
BEVEL_WIDTH = 0.012                                    # metres; catches light on edges = "real" look

# name: (sRGB hex, metallic, smoothness, emission strength)
PALETTE = {
    "M_PaintedMetal": ("4A4F55", 0.6, 0.35, 0.0),
    "M_DarkMetal":    ("2B2E33", 0.8, 0.30, 0.0),
    "M_BareMetal":    ("8A8D90", 1.0, 0.55, 0.0),
    "M_Rubber":       ("1E1F21", 0.0, 0.20, 0.0),
    "M_Pipe":         ("6B6252", 0.0, 0.25, 0.0),
    "M_Warning":      ("C9A227", 0.3, 0.40, 0.0),
    "M_Glass":        ("0C1418", 0.0, 0.90, 0.0),
    "M_LightCold":    ("DCE8FF", 0.0, 0.50, 3.0),
    "M_LightAmber":   ("FF9A3D", 0.0, 0.50, 2.5),
    "M_LightRed":     ("FF2A1A", 0.0, 0.50, 2.0),
    "M_Screen":       ("4DFF88", 0.0, 0.80, 1.2),
}


# --- Materials --------------------------------------------------------------

def srgb_to_linear(c):
    """Blender stores colours in linear space; the palette is written in sRGB like in Unity."""
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_to_linear(hex_rgb):
    return tuple(srgb_to_linear(int(hex_rgb[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


def set_input(node, names, value):
    """Socket names differ between Blender versions (e.g. 'Emission' vs 'Emission Color')."""
    for name in names:
        if name in node.inputs:
            node.inputs[name].default_value = value
            return


def build_palette():
    for name, (hex_rgb, metallic, smoothness, emission) in PALETTE.items():
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        try:
            mat.use_nodes = True
        except AttributeError:
            pass  # always on in newer Blender
        color = hex_to_linear(hex_rgb)
        bsdf = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        set_input(bsdf, ["Base Color"], (*color, 1.0))
        set_input(bsdf, ["Metallic"], metallic)
        set_input(bsdf, ["Roughness"], 1.0 - smoothness)
        if emission > 0:
            set_input(bsdf, ["Emission Color", "Emission"], (*color, 1.0))
            set_input(bsdf, ["Emission Strength"], emission)
        mat.diffuse_color = (*color, 1.0)  # solid viewport colour


# --- Geometry builder (Unity coordinates) ------------------------------------

class Piece:
    """Collects boxes, cylinders and bent pipes into one mesh with several materials."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.materials = []

    def _mat(self, name):
        if name not in self.materials:
            self.materials.append(name)
        return self.materials.index(name)

    def _tag(self, verts, mat):
        idx = self._mat(mat)
        for f in {f for v in verts for f in v.link_faces}:
            f.material_index = idx

    def box(self, mn, mx, mat, rot=None):
        """Axis-aligned box from min to max corner; rot = optional Euler degrees around its centre."""
        mn, mx = Vector(mn), Vector(mx)
        verts = bmesh.ops.create_cube(self.bm, size=1.0)["verts"]
        bmesh.ops.scale(self.bm, vec=mx - mn, verts=verts)
        if rot:
            r = Euler([math.radians(a) for a in rot], 'XYZ').to_matrix()
            bmesh.ops.rotate(self.bm, cent=(0, 0, 0), matrix=r, verts=verts)
        bmesh.ops.translate(self.bm, vec=(mn + mx) / 2, verts=verts)
        self._tag(verts, mat)

    def cylinder(self, start, end, radius, mat, segments=16):
        start, end = Vector(start), Vector(end)
        axis = end - start
        verts = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=segments,
                                      radius1=radius, radius2=radius, depth=axis.length)["verts"]
        rot = Vector((0, 0, 1)).rotation_difference(axis.normalized()).to_matrix()
        bmesh.ops.rotate(self.bm, cent=(0, 0, 0), matrix=rot, verts=verts)
        bmesh.ops.translate(self.bm, vec=(start + end) / 2, verts=verts)
        self._tag(verts, mat)

    def elbow(self, bend_radius, radius, mat, steps=10, segments=16):
        """90 degree pipe bend: starts at origin heading +Z, ends heading +Y."""
        rings = []
        for i in range(steps + 1):
            a = (math.pi / 2) * i / steps
            centre = Vector((0, bend_radius - bend_radius * math.cos(a), bend_radius * math.sin(a)))
            normal = Vector((0, math.cos(a), -math.sin(a)))
            side = Vector((1, 0, 0))
            ring = []
            for s in range(segments):
                phi = 2 * math.pi * s / segments
                ring.append(self.bm.verts.new(centre + radius * (math.cos(phi) * side + math.sin(phi) * normal)))
            rings.append(ring)
        faces = []
        for i in range(steps):
            for s in range(segments):
                a, b = rings[i][s], rings[i][(s + 1) % segments]
                c, d = rings[i + 1][(s + 1) % segments], rings[i + 1][s]
                faces.append(self.bm.faces.new((a, b, c, d)))
        faces.append(self.bm.faces.new(list(reversed(rings[0]))))
        faces.append(self.bm.faces.new(rings[-1]))
        idx = self._mat(mat)
        for f in faces:
            f.material_index = idx

    def build(self, collection, location):
        bm = self.bm
        bm.normal_update()
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

        # Unity (x, y, z) -> Blender (-x, -z, y). This is a mirror, so faces get flipped back.
        for v in bm.verts:
            x, y, z = v.co
            v.co = Vector((-x, -z, y))
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
        bm.normal_update()

        # Box-mapped UVs, 1 UV unit = 1 metre (tiling textures later "just work")
        uv = bm.loops.layers.uv.new("UVMap")
        for f in bm.faces:
            n = f.normal
            ax = max(range(3), key=lambda i: abs(n[i]))
            for loop in f.loops:
                co = loop.vert.co
                loop[uv].uv = (co.y, co.z) if ax == 0 else (co.x, co.z) if ax == 1 else (co.x, co.y)

        mesh = bpy.data.meshes.new(self.name)
        bm.to_mesh(mesh)
        bm.free()
        for m in self.materials:
            mesh.materials.append(bpy.data.materials[m])
        for p in mesh.polygons:
            p.use_smooth = True
        if hasattr(mesh, "use_auto_smooth"):          # Blender < 4.1 needs it for hardened normals
            mesh.use_auto_smooth = True
            mesh.auto_smooth_angle = math.radians(40)

        obj = bpy.data.objects.new(self.name, mesh)
        collection.objects.link(obj)
        bevel = obj.modifiers.new("Bevel", 'BEVEL')
        bevel.width = BEVEL_WIDTH
        bevel.segments = 2
        bevel.limit_method = 'ANGLE'
        bevel.angle_limit = math.radians(40)
        bevel.use_clamp_overlap = True
        bevel.harden_normals = True
        obj.location = location
        return obj


# --- Kit pieces (Unity metres: corridor 3 m wide, 3 m high, modules 2 m long along +Z) ---

def floor_2m():
    p = Piece("KIT_Floor_2m")
    p.box((-1.7, -0.2, 0), (-0.3, 0, 2), "M_DarkMetal")           # deck plates
    p.box((0.3, -0.2, 0), (1.7, 0, 2), "M_DarkMetal")
    p.box((-0.3, -0.2, 0), (0.3, -0.15, 2), "M_DarkMetal")        # service trench bottom
    p.cylinder((0, -0.1, 0), (0, -0.1, 2), 0.035, "M_Pipe", 12)   # cable in the trench
    for i in range(7):                                             # grating bars
        x = -0.3 + i * 0.1
        p.box((x - 0.012, -0.03, 0), (x + 0.012, 0, 2), "M_BareMetal")
    for z0 in (0.05, 1.05):                                        # rubber mats
        p.box((-1.4, 0, z0), (-0.4, 0.02, z0 + 0.9), "M_Rubber")
        p.box((0.4, 0, z0), (1.4, 0.02, z0 + 0.9), "M_Rubber")
    p.box((1.42, 0, 0), (1.47, 0.006, 2), "M_Warning")             # edge lines
    p.box((-1.47, 0, 0), (-1.42, 0.006, 2), "M_Warning")
    return p


def wall_base(p):
    """Right-hand wall (inner face at x = 1.5). Left wall = same piece turned 180 degrees in Unity."""
    p.box((1.5, 0, 0), (1.7, 3, 2), "M_DarkMetal")                 # structure
    p.box((1.38, 0, 0), (1.5, 0.3, 2), "M_DarkMetal")              # kick plate
    for z0, z1 in ((0.08, 0.97), (1.03, 1.92)):
        for y0, y1 in ((0.35, 1.6), (1.66, 2.58)):
            p.box((1.43, y0, z0), (1.5, y1, z1), "M_PaintedMetal")  # raised panels
            for y in (y0 + 0.06, y1 - 0.06):
                for z in (z0 + 0.06, z1 - 0.06):
                    p.cylinder((1.43, y, z), (1.415, y, z), 0.012, "M_BareMetal", 8)  # bolts
    p.box((1.45, 0.3, 0.97), (1.5, 2.62, 1.03), "M_BareMetal")      # vertical seam
    p.box((1.3, 2.62, 0), (1.5, 2.65, 2), "M_BareMetal")            # cable tray
    p.box((1.3, 2.65, 0), (1.32, 2.72, 2), "M_BareMetal")
    p.cylinder((1.38, 2.68, 0), (1.38, 2.68, 2), 0.02, "M_Rubber", 8)  # cables
    p.cylinder((1.42, 2.68, 0), (1.42, 2.68, 2), 0.015, "M_Warning", 8)


def wall_2m():
    p = Piece("KIT_Wall_2m")
    wall_base(p)
    return p


def wall_2m_pipes():
    p = Piece("KIT_Wall_2m_Pipes")
    wall_base(p)
    p.cylinder((1.3, 1.9, 0), (1.3, 1.9, 2), 0.07, "M_Pipe")
    p.cylinder((1.33, 2.17, 0), (1.33, 2.17, 2), 0.05, "M_BareMetal")
    for z in (0.5, 1.5):                                            # clamps
        p.box((1.2, 1.8, z - 0.03), (1.43, 2.25, z + 0.03), "M_BareMetal")
    return p


def ceiling_base(p):
    p.box((-1.7, 3, 0), (1.7, 3.2, 2), "M_DarkMetal")              # slab
    p.box((-1.5, 2.82, 0), (1.5, 3, 0.12), "M_DarkMetal")          # half beams (full beam when tiled)
    p.box((-1.5, 2.82, 1.88), (1.5, 3, 2), "M_DarkMetal")
    p.box((-1.5, 2.7, 0), (-1.0, 2.82, 2), "M_PaintedMetal")       # air duct
    for z in (0.5, 1.0, 1.5):
        p.box((-1.52, 2.68, z - 0.02), (-0.98, 2.84, z + 0.02), "M_BareMetal")


def ceiling_2m_light():
    p = Piece("KIT_Ceiling_2m_Light")
    ceiling_base(p)
    p.box((-0.25, 2.9, 0.3), (0.25, 3, 1.7), "M_DarkMetal")        # lamp housing
    p.box((-0.2, 2.885, 0.35), (0.2, 2.9, 1.65), "M_LightCold")    # light surface
    return p


def ceiling_2m_plain():
    p = Piece("KIT_Ceiling_2m_Plain")
    ceiling_base(p)
    p.box((-0.25, 2.9, 0.3), (0.25, 3, 1.7), "M_DarkMetal")        # dead lamp housing
    return p


def bulkhead():
    p = Piece("KIT_Bulkhead")
    for s in (-1, 1):
        x0, x1 = sorted((s * 1.5, s * 1.2))
        p.box((x0, 0, -0.15), (x1, 3, 0.15), "M_DarkMetal")        # pillars
        p.box((s * 1.2 - 0.2, 2.4, -0.15), (s * 1.2 + 0.2, 2.8, 0.15), "M_DarkMetal", rot=(0, 0, 45))  # corner chamfers
        inner = s * 1.2
        for i in range(5):                                         # warning bands, bottom of pillar
            y0 = 0.1 + i * 0.18
            mat = "M_Warning" if i % 2 == 0 else "M_DarkMetal"
            xa, xb = sorted((inner, inner - s * 0.01))
            p.box((xa, y0, -0.14), (xb, y0 + 0.18, 0.14), mat)
    p.box((-1.5, 2.6, -0.15), (1.5, 3, 0.15), "M_DarkMetal")       # top beam
    p.box((-0.12, 2.5, -0.06), (0.12, 2.6, 0.06), "M_DarkMetal")   # emergency lamp
    p.box((-0.09, 2.47, -0.04), (0.09, 2.5, 0.04), "M_LightRed")
    return p


def door_frame():
    p = Piece("KIT_Door_Frame")
    p.box((-1.5, 0, -0.1), (-0.8, 3, 0.1), "M_PaintedMetal")       # wall around the door
    p.box((0.8, 0, -0.1), (1.5, 3, 0.1), "M_PaintedMetal")
    p.box((-0.8, 2.3, -0.1), (0.8, 3, 0.1), "M_PaintedMetal")
    p.box((-0.92, 0, -0.16), (-0.8, 2.42, 0.16), "M_DarkMetal")    # frame
    p.box((0.8, 0, -0.16), (0.92, 2.42, 0.16), "M_DarkMetal")
    p.box((-0.92, 2.3, -0.16), (0.92, 2.42, 0.16), "M_DarkMetal")
    p.box((-0.6, 2.43, 0.1), (0.6, 2.53, 0.17), "M_Warning")       # warning header
    p.box((-0.1, 2.6, 0.1), (0.1, 2.7, 0.2), "M_LightAmber")        # status lamp
    p.box((0.98, 1.1, 0.1), (1.22, 1.5, 0.2), "M_DarkMetal")       # door control panel
    p.box((1.02, 1.25, 0.2), (1.18, 1.45, 0.21), "M_Screen")
    return p


def door_panel():
    p = Piece("KIT_Door_Panel")
    p.box((-0.8, 0, -0.05), (0.8, 2.3, 0.05), "M_PaintedMetal")
    for y in (0.5, 1.2):
        p.box((-0.75, y, -0.07), (0.75, y + 0.08, 0.07), "M_BareMetal")  # reinforcement ribs
    p.box((-0.15, 1.5, -0.06), (0.15, 1.85, 0.06), "M_Glass")
    p.box((-0.8, 0, -0.06), (-0.7, 2.3, 0.06), "M_Warning")        # leading edge
    return p


def pipe_straight():
    p = Piece("KIT_Pipe_Straight_2m")
    p.cylinder((0, 0, 0), (0, 0, 2), 0.08, "M_Pipe")
    for z in (0.02, 1.98):
        p.cylinder((0, 0, z - 0.02), (0, 0, z + 0.02), 0.11, "M_BareMetal")
    return p


def pipe_elbow():
    p = Piece("KIT_Pipe_Elbow")
    p.elbow(0.3, 0.08, "M_Pipe")
    p.cylinder((0, 0, -0.02), (0, 0, 0.02), 0.11, "M_BareMetal")
    p.cylinder((0, 0.28, 0.3), (0, 0.32, 0.3), 0.11, "M_BareMetal")
    return p


def wall_lamp():
    p = Piece("KIT_WallLamp")                                       # back on the wall (z = 0), shines along +Z
    p.box((-0.1, -0.12, 0), (0.1, 0.12, 0.05), "M_DarkMetal")
    p.cylinder((0, 0, 0.05), (0, 0, 0.14), 0.07, "M_LightAmber")
    for a in (0, 60, 120):                                          # protective cage
        p.box((-0.085, -0.008, 0.05), (0.085, 0.008, 0.16), "M_BareMetal", rot=(0, 0, a))
    return p


def console():
    p = Piece("KIT_Console")                                        # back on the wall (z = 0), faces +Z
    p.box((-0.3, 0.9, 0), (0.3, 1.6, 0.18), "M_DarkMetal")
    p.box((-0.25, 1.2, 0.17), (0.25, 1.5, 0.2), "M_Screen")
    p.box((-0.3, 0.9, 0.1), (0.3, 1.1, 0.3), "M_PaintedMetal", rot=(-25, 0, 0))  # keyboard ledge
    for i, mat in enumerate(("M_LightAmber", "M_Warning", "M_LightRed")):
        x = -0.15 + i * 0.15
        p.box((x - 0.03, 1.13, 0.2), (x + 0.03, 1.16, 0.24), mat)
    return p


def crate():
    p = Piece("KIT_Crate_1m")
    p.box((-0.47, 0.03, -0.47), (0.47, 0.97, 0.47), "M_PaintedMetal")
    t = 0.05
    for x in (-0.5, 0.5 - t):
        for z in (-0.5, 0.5 - t):
            p.box((x, 0, z), (x + t, 1, z + t), "M_DarkMetal")       # vertical edges
    for y in (0, 1 - t):
        for x in (-0.5, 0.5 - t):
            p.box((x, y, -0.5), (x + t, y + t, 0.5), "M_DarkMetal")  # edges along Z
        for z in (-0.5, 0.5 - t):
            p.box((-0.5, y, z), (0.5, y + t, z + t), "M_DarkMetal")  # edges along X
    p.box((-0.2, 0.55, 0.47), (0.2, 0.75, 0.48), "M_Warning")       # label
    return p


PIECES = [floor_2m, wall_2m, wall_2m_pipes, ceiling_2m_light, ceiling_2m_plain, bulkhead,
          door_frame, door_panel, pipe_straight, pipe_elbow, wall_lamp, console, crate]


# --- Export -------------------------------------------------------------------

def get_clean_collection():
    coll = bpy.data.collections.get(COLLECTION_NAME)
    if coll is None:
        coll = bpy.data.collections.new(COLLECTION_NAME)
        bpy.context.scene.collection.children.link(coll)
    for obj in list(coll.objects):                                  # re-running replaces old pieces
        bpy.data.objects.remove(obj, do_unlink=True)
    return coll


def export_fbx(obj, folder):
    """Each piece at the origin, alone in the selection, Unity-friendly axes and scale."""
    view_layer = bpy.context.view_layer
    for o in view_layer.objects:
        o.select_set(False)
    obj.select_set(True)
    view_layer.objects.active = obj

    shown_at = obj.location.copy()
    obj.location = (0, 0, 0)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(folder, obj.name + ".fbx"),
        use_selection=True,
        object_types={'MESH'},
        use_mesh_modifiers=True,
        mesh_smooth_type='OFF',
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode='AUTO',
    )
    obj.location = shown_at


def main():
    if not os.path.isdir(PROJECT_DIR):
        raise RuntimeError(f"Nie znaleziono projektu Unity: {PROJECT_DIR}. Popraw PROJECT_DIR na górze skryptu.")

    export_dir = os.path.join(PROJECT_DIR, EXPORT_SUBDIR)
    os.makedirs(export_dir, exist_ok=True)

    build_palette()
    coll = get_clean_collection()

    # Lay pieces out in a row so they are easy to inspect in Blender
    for i, make in enumerate(PIECES):
        obj = make().build(coll, (i * 4.0, 0, 0))
        export_fbx(obj, export_dir)
        print(f"[Kotwica] Eksport: {obj.name}.fbx")

    os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print(f"[Kotwica] Gotowe: {len(PIECES)} elementów w {export_dir}")


main()
