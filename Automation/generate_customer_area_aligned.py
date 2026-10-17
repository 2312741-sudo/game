#!/usr/bin/env python3
"""
Generates Assets/TramChanh/Prefabs/CustomerArea/PF_CustomerArea_10Tables.prefab
with 100% valid Unity YAML:
- Preserves root GameObject ID 5834469491352108292 and root Transform ID 6278211215198632772
  so that PF_TramChanh_StreetExtended, PF_TramChanh_Street, SCN_Art_EnvironmentPreview
  and PF_AccelRoadsideEnvironment properly link the CustomerArea nested prefab.
- Properly generates stripped Transform nodes (!u!4 &... stripped) for every nested
  PrefabInstance (PF_YellowCrateTable and PF_PlasticStool), and references their fileIDs
  in each table's m_Children array.
- Positions 10 tables in neat, aligned rows (Rotation 0 deg):
  * Row 1 (West upper, Z = +1.40m): TABLE_01 (-2.20), TABLE_02 (-3.70), TABLE_03 (-5.20), TABLE_04 (-6.70)
  * Row 2 (West lower, Z = -1.40m): TABLE_05 (-2.20), TABLE_06 (-4.45), TABLE_07 (-6.70)
  * Row 3 (East opp.,   X = +4.50m): TABLE_08 (Z=+1.40), TABLE_09 (Z=0.00), TABLE_10 (Z=-1.40)
- Places 2 to 4 PF_PlasticStool instances symmetrically around each table (32 stools total)
  using the new beige plastic stool model from 47851875f5b35d81faa5f5067f2f70f4.fbx.
- Places SM_TableSign_xx on the stainless tray and SM_TableBillboard_xx floating above each table.
"""

import os

TABLE_SPECS = [
    # id, pos_x, pos_z, stool_count
    ("TABLE_01", -2.20,  1.40, 4),
    ("TABLE_02", -3.70,  1.40, 3),
    ("TABLE_03", -5.20,  1.40, 4),
    ("TABLE_04", -6.70,  1.40, 2),
    ("TABLE_05", -2.20, -1.40, 3),
    ("TABLE_06", -4.45, -1.40, 4),
    ("TABLE_07", -6.70, -1.40, 3),
    ("TABLE_08",  4.50,  1.40, 4),
    ("TABLE_09",  4.50,  0.00, 3),
    ("TABLE_10",  4.50, -1.40, 2),
]

SIGN_GUIDS = [
    "01dc17d6799b4389b70a465b2a5af2b1",
    "fb4379f417534be68f865be54a3c3338",
    "1403a69c13514d4f98ae01af7588d460",
    "f355468dbb724fce9c73354f0c40c134",
    "6e461ebb09ff4718ab2b3b5571a3f11b",
    "5bc0639b7fe64c26903752ecd13298de",
    "6762435bd43d4cd5a721b47d41f94a95",
    "a7dfc25a1508449fac45c2425658452c",
    "927219c55c9747bbae4489a84ba8543c",
    "b4bf8b7b896741acabdf9dedc2fe5595",
]

BILLBOARD_GUIDS = [
    "671b5512ad0842d19dd35acd9731fbaa",
    "b0425fc1c5684296831f30a93c0fb35d",
    "7cf774248e96482997f8a1c64d6bf6b8",
    "adf30e50521042038609314b18581ae2",
    "06c10999118f45c18a9b8bd83e5ae74d",
    "63755aa90e7f46bc8ad887a19f81f7bd",
    "3febfd5ddd2d43e9b8471f99245c1fa0",
    "e1c051c91c0d4faabc2d8aa6037fa754",
    "0f39ea957456400ab2304e4d11bec1bf",
    "d0fac84bfa4343d2b6d3334a586e63d7",
]

CRATE_TABLE_PREFAB_GUID = "cc5a65b6c11f8421c882dcaac7f8590a"
CRATE_ROOT_TRANSFORM_ID = 4464241318907377179
CRATE_ROOT_GO_ID = 3299185173858904332

STOOL_PREFAB_GUID = "be0630dbaa2dd448abde9bed48e119b9"
STOOL_ROOT_TRANSFORM_ID = 6860047657734581738
STOOL_ROOT_GO_ID = 5023653642298891178

TABLE_SIGN_MAT_GUID = "eb82a817c7994cd5bbed7f9c56aafcae"
TABLE_BILLBOARD_MAT_GUID = "314a5d97b8bb45378332ed1c8b0600b4"

ROOT_GO_ID = 5834469491352108292
ROOT_TR_ID = 6278211215198632772

def main():
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
    ]

    table_tr_ids = []

    # Prepare blocks
    blocks = []

    for t_idx, (t_id, px, pz, s_count) in enumerate(TABLE_SPECS):
        num = t_idx + 1
        base_id = 1000000000000000000 + num * 1000

        t_go_id = base_id + 1
        t_tr_id = base_id + 2
        table_tr_ids.append(t_tr_id)

        seat_go_id = base_id + 3
        seat_tr_id = base_id + 4

        serv_go_id = base_id + 5
        serv_tr_id = base_id + 6

        sign_go_id = base_id + 7
        sign_tr_id = base_id + 8
        sign_mf_id = base_id + 9
        sign_mr_id = base_id + 10

        bill_go_id = base_id + 11
        bill_tr_id = base_id + 12
        bill_mf_id = base_id + 13
        bill_mr_id = base_id + 14

        crate_inst_id = base_id + 20
        crate_strip_id = base_id + 21

        stool_inst_ids = []
        stool_strip_ids = []
        # Stool definitions relative to table
        # 0: West (-0.48, 0, 0), rot: yaw 90 deg (0, 0.7071068, 0, 0.7071068)
        # 1: East (+0.48, 0, 0), rot: yaw -90 deg (0, -0.7071068, 0, 0.7071068)
        # 2: North (0, 0, +0.42), rot: yaw 180 deg (0, 1, 0, 0)
        # 3: South (0, 0, -0.42), rot: yaw 0 deg (0, 0, 0, 1)
        stool_poses = [
            (-0.48, 0.0,  0.00,  0.0,  0.7071068, 0.0, 0.7071068),
            ( 0.48, 0.0,  0.00,  0.0, -0.7071068, 0.0, 0.7071068),
            ( 0.00, 0.0,  0.42,  0.0,  1.0,       0.0, 0.0),
            ( 0.00, 0.0, -0.42,  0.0,  0.0,       0.0, 1.0),
        ]

        for s_i in range(s_count):
            s_inst = base_id + 30 + s_i * 2
            s_strip = base_id + 30 + s_i * 2 + 1
            stool_inst_ids.append(s_inst)
            stool_strip_ids.append(s_strip)

        # Children of table transform:
        t_children = [seat_tr_id, serv_tr_id, sign_tr_id, bill_tr_id, crate_strip_id] + stool_strip_ids

        # 1. TABLE_xx GameObject & Transform
        blocks.append(f"""--- !u!1 &{t_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {t_tr_id}}}
  m_Layer: 8
  m_Name: {t_id}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1""")

        children_yaml = "\n".join([f"  - {{fileID: {cid}}}" for cid in t_children])
        blocks.append(f"""--- !u!4 &{t_tr_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {t_go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: {px}, y: 0.0, z: {pz}}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{children_yaml}
  m_Father: {{fileID: {ROOT_TR_ID}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}""")

        # 2. Seat anchor
        blocks.append(f"""--- !u!1 &{seat_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {seat_tr_id}}}
  m_Layer: 8
  m_Name: Seat
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1""")

        blocks.append(f"""--- !u!4 &{seat_tr_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {seat_go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0.7071068, z: 0, w: 0.7071068}}
  m_LocalPosition: {{x: -0.48, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {t_tr_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}""")

        # 3. ServicePoint anchor
        blocks.append(f"""--- !u!1 &{serv_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {serv_tr_id}}}
  m_Layer: 8
  m_Name: ServicePoint
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1""")

        blocks.append(f"""--- !u!4 &{serv_tr_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {serv_go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0.8}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {t_tr_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}""")

        # 4. SM_TableSign_xx
        sign_mesh_guid = SIGN_GUIDS[t_idx]
        blocks.append(f"""--- !u!1 &{sign_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {sign_tr_id}}}
  - component: {{fileID: {sign_mf_id}}}
  - component: {{fileID: {sign_mr_id}}}
  m_Layer: 8
  m_Name: SM_TableSign_{num:02d}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1""")

        blocks.append(f"""--- !u!4 &{sign_tr_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {sign_go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0.12, y: 0.342, z: 0.10}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {t_tr_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}""")

        blocks.append(f"""--- !u!33 &{sign_mf_id}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {sign_go_id}}}
  m_Mesh: {{fileID: -2432090755550338912, guid: {sign_mesh_guid}, type: 3}}""")

        blocks.append(f"""--- !u!23 &{sign_mr_id}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {sign_go_id}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_Materials:
  - {{fileID: 2100000, guid: {TABLE_SIGN_MAT_GUID}, type: 2}}""")

        # 5. SM_TableBillboard_xx
        bill_mesh_guid = BILLBOARD_GUIDS[t_idx]
        blocks.append(f"""--- !u!1 &{bill_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {bill_tr_id}}}
  - component: {{fileID: {bill_mf_id}}}
  - component: {{fileID: {bill_mr_id}}}
  m_Layer: 8
  m_Name: SM_TableBillboard_{num:02d}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1""")

        blocks.append(f"""--- !u!4 &{bill_tr_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {bill_go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0.85, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {t_tr_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}""")

        blocks.append(f"""--- !u!33 &{bill_mf_id}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {bill_go_id}}}
  m_Mesh: {{fileID: -2432090755550338912, guid: {bill_mesh_guid}, type: 3}}""")

        blocks.append(f"""--- !u!23 &{bill_mr_id}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {bill_go_id}}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_Materials:
  - {{fileID: 2100000, guid: {TABLE_BILLBOARD_MAT_GUID}, type: 2}}""")

        # 6. PF_YellowCrateTable PrefabInstance + stripped Transform
        blocks.append(f"""--- !u!1001 &{crate_inst_id}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {t_tr_id}}}
    m_Modifications:
    - target: {{fileID: {CRATE_ROOT_TRANSFORM_ID}, guid: {CRATE_TABLE_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: {CRATE_ROOT_TRANSFORM_ID}, guid: {CRATE_TABLE_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: {CRATE_ROOT_TRANSFORM_ID}, guid: {CRATE_TABLE_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: {CRATE_ROOT_TRANSFORM_ID}, guid: {CRATE_TABLE_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.w
      value: 1
      objectReference: {{fileID: 0}}
    - target: {{fileID: {CRATE_ROOT_TRANSFORM_ID}, guid: {CRATE_TABLE_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: {CRATE_ROOT_TRANSFORM_ID}, guid: {CRATE_TABLE_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: {CRATE_ROOT_TRANSFORM_ID}, guid: {CRATE_TABLE_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: {CRATE_ROOT_GO_ID}, guid: {CRATE_TABLE_PREFAB_GUID}, type: 3}}
      propertyPath: m_Name
      value: PF_YellowCrateTable
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {CRATE_TABLE_PREFAB_GUID}, type: 3}}""")

        blocks.append(f"""--- !u!4 &{crate_strip_id} stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: {CRATE_ROOT_TRANSFORM_ID}, guid: {CRATE_TABLE_PREFAB_GUID}, type: 3}}
  m_PrefabInstance: {{fileID: {crate_inst_id}}}
  m_PrefabAsset: {{fileID: 0}}""")

        # 7. PF_PlasticStool PrefabInstances + stripped Transforms
        for s_i in range(s_count):
            s_inst = stool_inst_ids[s_i]
            s_strip = stool_strip_ids[s_i]
            sx, sy, sz, rx, ry, rz, rw = stool_poses[s_i]

            blocks.append(f"""--- !u!1001 &{s_inst}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {t_tr_id}}}
    m_Modifications:
    - target: {{fileID: {STOOL_ROOT_TRANSFORM_ID}, guid: {STOOL_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: {sx}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {STOOL_ROOT_TRANSFORM_ID}, guid: {STOOL_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: {sy}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {STOOL_ROOT_TRANSFORM_ID}, guid: {STOOL_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: {sz}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {STOOL_ROOT_TRANSFORM_ID}, guid: {STOOL_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.x
      value: {rx}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {STOOL_ROOT_TRANSFORM_ID}, guid: {STOOL_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.y
      value: {ry}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {STOOL_ROOT_TRANSFORM_ID}, guid: {STOOL_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.z
      value: {rz}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {STOOL_ROOT_TRANSFORM_ID}, guid: {STOOL_PREFAB_GUID}, type: 3}}
      propertyPath: m_LocalRotation.w
      value: {rw}
      objectReference: {{fileID: 0}}
    - target: {{fileID: {STOOL_ROOT_GO_ID}, guid: {STOOL_PREFAB_GUID}, type: 3}}
      propertyPath: m_Name
      value: PF_PlasticStool
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {STOOL_PREFAB_GUID}, type: 3}}""")

            blocks.append(f"""--- !u!4 &{s_strip} stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: {STOOL_ROOT_TRANSFORM_ID}, guid: {STOOL_PREFAB_GUID}, type: 3}}
  m_PrefabInstance: {{fileID: {s_inst}}}
  m_PrefabAsset: {{fileID: 0}}""")

    # Finally, CustomerArea Root GameObject and Transform
    root_kids_yaml = "\n".join([f"  - {{fileID: {tid}}}" for tid in table_tr_ids])
    blocks.append(f"""--- !u!1 &{ROOT_GO_ID}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {ROOT_TR_ID}}}
  m_Layer: 8
  m_Name: CustomerArea
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1""")

    blocks.append(f"""--- !u!4 &{ROOT_TR_ID}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ROOT_GO_ID}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{root_kids_yaml}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}""")

    content = "\n".join(lines) + "\n" + "\n".join(blocks) + "\n"
    target_path = "Assets/TramChanh/Prefabs/CustomerArea/PF_CustomerArea_10Tables.prefab"
    with open(target_path, "w") as f:
        f.write(content)
    print(f"Generated {target_path} successfully ({len(blocks)} blocks).")

if __name__ == "__main__":
    main()
