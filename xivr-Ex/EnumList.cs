﻿using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;

namespace xivr
{
    [Flags]
    public enum poseType
    {
        None = 0,
        Projection = 1,
        EyeOffset = 5,
        hmdPosition = 10,
        LeftHand = 20,
        LeftHandPalm = 21,
        RightHand = 30,
        RightHandPalm = 31,
    }

    [Flags]
    public enum RenderModes
    {
        None,
        AlternatEye,
        TwoD,
        SBS
    };

    [Flags]
    public enum CharEquipSlots : uint
    {
        Head = 0,
        Body = 1,
        Hands = 2,
        Legs = 3,
        Feet = 4,
        Ears = 5,
        Neck = 6,
        Wrist = 7,
        RRing = 8,
        LRing = 9
    }

    [Flags]
    public enum CharWeaponSlots
    {
        MainHand = 0,
        OffHand = 1,
        uk3 = 2
    }

    [Flags]
    public enum CameraModes
    {
        None = -1,
        FirstPerson = 0,
        ThirdPerson = 1,
    }

    [Flags]
    public enum ModelCullTypes : byte
    {
        None = 0,
        InsideCamera = 0x42,
        OutsideCullCone = 0x43,
        Visible = 0x4B
    }

    public struct BoneData
    {
        public hkVector4f Position;
        public hkQuaternionf Rotation;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct fingerHandLayout
    {
        [FieldOffset(0x00)] public BoneData root;
        [FieldOffset(0x20)] public BoneData wrist;

        [FieldOffset(0x40)] public BoneData thumb0Metacarpal;
        [FieldOffset(0x60)] public BoneData thumb1Proximal;
        [FieldOffset(0x60)] public BoneData thumb2Middle;
        [FieldOffset(0x80)] public BoneData thumb3Distal;
        [FieldOffset(0xA0)] public BoneData thumb4Tip;

        [FieldOffset(0xC0)] public BoneData index0Metacarpal;
        [FieldOffset(0xE0)] public BoneData index1Proximal;
        [FieldOffset(0x100)] public BoneData index2Middle;
        [FieldOffset(0x120)] public BoneData index3Distal;
        [FieldOffset(0x140)] public BoneData index4Tip;

        [FieldOffset(0x160)] public BoneData middle0Metacarpal;
        [FieldOffset(0x180)] public BoneData middle1Proximal;
        [FieldOffset(0x1A0)] public BoneData middle2Middle;
        [FieldOffset(0x1C0)] public BoneData middle3Distal;
        [FieldOffset(0x1E0)] public BoneData middle4Tip;

        [FieldOffset(0x200)] public BoneData ring0Metacarpal;
        [FieldOffset(0x220)] public BoneData ring1Proximal;
        [FieldOffset(0x240)] public BoneData ring2Middle;
        [FieldOffset(0x260)] public BoneData ring3Distal;
        [FieldOffset(0x280)] public BoneData ring4Tip;

        [FieldOffset(0x2A0)] public BoneData pinky0Metacarpal;
        [FieldOffset(0x2C0)] public BoneData pinky1Proximal;
        [FieldOffset(0x2E0)] public BoneData pinky2Middle;
        [FieldOffset(0x300)] public BoneData pinky3Distal;
        [FieldOffset(0x320)] public BoneData pinky4Tip;

        [FieldOffset(0x340)] public BoneData thumbAux;
        [FieldOffset(0x360)] public BoneData indexAux;
        [FieldOffset(0x380)] public BoneData middleAux;
        [FieldOffset(0x3A0)] public BoneData ringAux;
        [FieldOffset(0x3C0)] public BoneData pinkyAux;
    }




    [Flags]
    public enum VisibilityFlags
    {
        None = 0,
        Unknown0 = 1 << 0,
        Model = 1 << 1,
        Unknown2 = 1 << 2,
        Unknown3 = 1 << 3,
        Unknown4 = 1 << 4,
        Unknown5 = 1 << 5,
        Unknown6 = 1 << 6,
        Unknown7 = 1 << 7,
        Unknown8 = 1 << 8,
        Unknown9 = 1 << 9,
        Unknown10 = 1 << 10,
        Nameplate = 1 << 11,
        Unknown12 = 1 << 12,
        Unknown13 = 1 << 13,
        Unknown14 = 1 << 14,
        Unknown15 = 1 << 15,
        Unknown16 = 1 << 16,
        Unknown17 = 1 << 17,
        Unknown18 = 1 << 18,
        Unknown19 = 1 << 19,
        Unknown20 = 1 << 20,
        Unknown21 = 1 << 21,
        Unknown22 = 1 << 22,
        Unknown23 = 1 << 23,
        Unknown24 = 1 << 24,
        Unknown25 = 1 << 25,
        Unknown26 = 1 << 26,
        Unknown27 = 1 << 27,
        Unknown28 = 1 << 28,
        Unknown39 = 1 << 29,
        Unknown30 = 1 << 30,
        Unknown31 = 1 << 31,
        Invisible = Model | Nameplate,
    }

    [Flags]
    public enum LanguageTypes
    {
        en,
        jp
    }


    public enum BoneList
    {
        _unknown_,       //_unknown_,
        _root_,          //_root_,
        n_root,          //e_root,
        n_hara,          //e_abdomen,
        j_sebo_a,        //e_spine_a,
        j_sebo_b,        //e_spine_b,
        j_sebo_c,        //e_spine_c,
        j_mune_l,        //e_breast_l,
        j_mune_r,        //e_breast_r,
        j_kubi,          //e_neck,
        j_kao,           //e_head,
        j_ago,           //e_jaw,
        j_mimi_l,        //e_ear_l,
        j_mimi_r,        //e_ear_r,
        j_f_dmab_l,      //e_eyelid_l_l,
        j_f_dmab_r,      //e_eyelid_l_r,
        j_f_umab_l,      //e_eyelid_u_l,
        j_f_umab_r,      //e_eyelid_u_r,
        j_f_eye_l,       //e_eye_l,
        j_f_eye_r,       //e_eye_r,
        j_f_hana,        //e_noes,
        j_f_hoho_l,      //e_cheek_l,
        j_f_hoho_r,      //e_cheek_r,
        j_f_lip_l,       //e_lip_l,
        j_f_lip_r,       //e_lip_r,
        j_f_mayu_l,      //e_eyebrow_l,
        j_f_mayu_r,      //e_eyebrow_r,
        j_f_memoto,      //e_bridge,
        j_f_miken_l,     //e_brow_l,
        j_f_miken_r,     //e_brow_r,
        j_f_ulip_a,      //e_lip_u_a,
        j_f_dlip_a,      //e_lip_l_a,
        j_f_ulip_b,      //e_lip_u_b,
        j_f_dlip_b,      //e_lip_l_b,
        j_kami_a,        //e_hair_a,
        j_kami_b,        //e_hair_b,
        j_kami_f_l,      //e_hair_f_l,
        j_kami_f_r,      //e_hair_f_r,
        j_sako_l,        //e_collarbone_l,
        j_sako_r,        //e_collarbone_r,
        j_kosi,          //e_waist,
        j_asi_a_l,       //e_left_leg,
        j_asi_a_r,       //e_right_leg,
        j_asi_b_l,       //e_knee_l,
        j_asi_b_r,       //e_knee_r,
        j_asi_c_l,       //e_calf_l,
        j_asi_c_r,       //e_calf_r,
        j_asi_d_l,       //e_foot_l,
        j_asi_d_r,       //e_foot_r,
        j_asi_e_l,       //e_toes_l,
        j_asi_e_r,       //e_toes_r,
        j_ude_a_l,       //e_arm_l,
        j_ude_a_r,       //e_arm_r,
        j_ude_b_l,       //e_forearm_l,
        j_ude_b_r,       //e_forearm_r,
        j_te_l,          //e_hand_l,
        j_te_r,          //e_hand_r,
        n_hte_l,         //e_wrist_l,
        n_hte_r,         //e_wrist_r,
        j_hito_a_l,      //e_finger_index_a_l,
        j_hito_a_r,      //e_finger_index_a_r,
        j_ko_a_l,        //e_finger_pinky_a_l,
        j_ko_a_r,        //e_finger_pinky_a_r,
        j_kusu_a_l,      //e_finger_ring_a_l,
        j_kusu_a_r,      //e_finger_ring_a_r,
        j_naka_a_l,      //e_finger_middle_a_l,
        j_naka_a_r,      //e_finger_middle_a_r,
        j_oya_a_l,       //e_thumb_a_l,
        j_oya_a_r,       //e_thumb_a_r,
        j_hito_b_l,      //e_finger_index_b_l,
        j_hito_b_r,      //e_finger_index_b_r,
        j_ko_b_l,        //e_finger_pinky_b_l,
        j_ko_b_r,        //e_finger_pinky_b_r,
        j_kusu_b_l,      //e_finger_ring_b_l,
        j_kusu_b_r,      //e_finger_ring_b_r,
        j_naka_b_l,      //e_finger_middle_b_l,
        j_naka_b_r,      //e_finger_middle_b_r,
        j_hane_a_l,      //e_wing_a_l,
        j_hane_a_r,      //e_wing_a_r,
        j_hane_b_l,      //e_wing_b_l,
        j_hane_b_r,      //e_wing_b_r,
        j_hane_c_l,      //e_wing_c_l,
        j_hane_c_r,      //e_wing_c_r,
        j_hane_d_l,      //e_wing_d_l,
        j_hane_d_r,      //e_wing_d_r,
        j_hane_e_l,      //e_wing_e_l,
        j_hane_e_r,      //e_wing_e_r,
        j_hane_f_l,      //e_wing_f_l,
        j_hane_f_r,      //e_wing_f_r,
        j_hane_g_l,      //e_wing_g_l,
        j_hane_g_r,      //e_wing_g_r,
        j_hane_h_l,      //e_wing_h_l,
        j_hane_h_r,      //e_wing_h_r,
        j_hane_i_l,      //e_wing_i_l,
        j_hane_i_r,      //e_wing_i_r,
        j_hane_j_l,      //e_wing_j_l,
        j_hane_j_r,      //e_wing_j_r,
        j_oya_b_l,       //e_thumb_b_l,
        j_oya_b_r,       //e_thumb_b_r,
        n_sippo_a,       //e_tail_a,
        n_sippo_b,       //e_tail_b,
        n_sippo_c,       //e_tail_c,
        n_sippo_d,       //e_tail_d,
        n_sippo_e,       //e_tail_e,
        j_sippo_a,       //e_tail1_a,
        j_sippo_b,       //e_tail1_b,
        j_sippo_c,       //e_tail1_c,
        j_sippo_d,       //e_tail1_d,
        j_sippo_e,       //e_tail1_e,
        j_sippo_l,       //e_tail1_l,
        j_sippo_r,       //e_tail1_r,
        n_nimotu_a,      //e_load_a,
        n_nimotu_b,      //e_load_b,
        n_nimotu_c,      //e_load_c,
        n_nimotu_l,      //e_load_l,
        n_nimotu_r,      //e_load_r,
        n_throw,         //e_throw,
        n_hizasoubi_l,   //e_poleyn_l,
        n_hizasoubi_r,   //e_poleyn_r,
        n_kataarmor_l,   //e_pauldron_l,
        n_kataarmor_r,   //e_pauldron_r,
        n_hkata_l,       //e_shoulder_l,
        n_hkata_r,       //e_shoulder_r,
        n_buki_tate_l,   //e_shield_l,
        n_buki_tate_r,   //e_shield_r,
        n_ear_a_l,       //e_earing_a_l,
        n_ear_a_r,       //e_earing_a_r,
        n_ear_b_l,       //e_earing_b_l,
        n_ear_b_r,       //e_earing_b_r,
        n_hhiji_l,       //e_elbow_l,
        n_hhiji_r,       //e_elbow_r,
        n_hijisoubi_l,   //e_couter_l,
        n_hijisoubi_r,   //e_couter_r,
        n_buki_l,        //e_weapon_l,
        n_buki_r,        //e_weapon_r,
        j_buki2_kosi_l,  //e_holster_l,
        j_buki2_kosi_r,  //e_holster_r,
        j_buki_kosi_l,   //e_sheathe_l,
        j_buki_kosi_r,   //e_sheathe_r,
        j_buki_sebo_l,   //e_scabbard_l,
        j_buki_sebo_r,   //e_scabbard_r,
        j_sk_b_a_l,      //e_cloth_b_a_l,
        j_sk_b_a_r,      //e_cloth_b_a_r,
        j_sk_f_a_l,      //e_cloth_f_a_l,
        j_sk_f_a_r,      //e_cloth_f_a_r,
        j_sk_s_a_l,      //e_cloth_s_a_l,
        j_sk_s_a_r,      //e_cloth_s_a_r,
        j_sk_b_b_l,      //e_cloth_b_b_l,
        j_sk_b_b_r,      //e_cloth_b_b_r,
        j_sk_f_b_l,      //e_cloth_f_b_l,
        j_sk_f_b_r,      //e_cloth_f_b_r,
        j_sk_s_b_l,      //e_cloth_s_b_l,
        j_sk_s_b_r,      //e_cloth_s_b_r,
        j_sk_b_c_l,      //e_cloth_b_c_l,
        j_sk_b_c_r,      //e_cloth_b_c_r,
        j_sk_f_c_l,      //e_cloth_f_c_l,
        j_sk_f_c_r,      //e_cloth_f_c_r,
        j_sk_s_c_l,      //e_cloth_s_c_l,
        j_sk_s_c_r,      //e_cloth_s_c_r,
        n_mount,         //e_mount,
        n_mount_second,  //e_mount_second,
        j_ex_met_a,      //j_ex_met_a,
        j_ex_met_l,      //j_ex_met_l,
        j_ex_met_r,      //j_ex_met_r,
        j_ex_top_a,      //j_ex_top_a,
        j_ex_top_c,      //j_ex_top_c,
        j_ex_top_b,      //j_ex_top_b,
        j_ex_top_d,      //j_ex_top_d,
        j_zera_a_l,      //j_zera_a_l,
        j_zera_a_r,      //j_zera_a_r,
        j_zerb_a_l,      //j_zerb_a_l,
        j_zerb_a_r,      //j_zerb_a_r,
        j_zerc_a_l,      //j_zerc_a_l,
        j_zerc_a_r,      //j_zerc_a_r,
        j_zerd_a_l,      //j_zerd_a_l,
        j_zerd_a_r,      //j_zerd_a_r,
        j_zera_b_l,      //j_zera_b_l,
        j_zera_b_r,      //j_zera_b_r,
        j_zerb_b_l,      //j_zerb_b_l,
        j_zerb_b_r,      //j_zerb_b_r,
        j_zerc_b_l,      //j_zerc_b_l,
        j_zerc_b_r,      //j_zerc_b_r,
        j_zerd_b_l,      //j_zerd_b_l,
        j_zerd_b_r,      //j_zerd_b_r,
        j_zacc,          //j_zacc,
        j_f_hige_l,      //j_f_hige_l,
        j_f_hige_r,      //j_f_hige_r,
        j_f_uago,        //j_f_uago,
        j_f_ulip,        //j_f_ulip,
        n_f_lip_l,       //n_f_lip_l,
        n_f_lip_r,       //n_f_lip_r,
        n_f_ulip_l,      //n_f_ulip_l,
        n_f_ulip_r,      //n_f_ulip_r,
        j_f_dlip,        //j_f_dlip,
        iv_ko_c_l,       //iv_ko_c_l,
        iv_kusu_c_l,     //iv_kusu_c_l,
        iv_naka_c_l,     //iv_naka_c_l,
        iv_hito_c_l,     //iv_hito_c_l,
        iv_ko_c_r,       //iv_ko_c_r,
        iv_kusu_c_r,     //iv_kusu_c_r,
        iv_naka_c_r,     //iv_naka_c_r,
        iv_hito_c_r,     //iv_hito_c_r,
        iv_asi_oya_a_l,  //iv_asi_oya_a_l,
        iv_asi_oya_b_l,  //iv_asi_oya_b_l,
        iv_asi_hito_a_l, //iv_asi_hito_a_l,
        iv_asi_hito_b_l, //iv_asi_hito_b_l,
        iv_asi_naka_a_l, //iv_asi_naka_a_l,
        iv_asi_naka_b_l, //iv_asi_naka_b_l,
        iv_asi_kusu_a_l, //iv_asi_kusu_a_l,
        iv_asi_kusu_b_l, //iv_asi_kusu_b_l,
        iv_asi_ko_a_l,   //iv_asi_ko_a_l,
        iv_asi_ko_b_l,   //iv_asi_ko_b_l,
        iv_asi_oya_a_r,  //iv_asi_oya_a_r,
        iv_asi_oya_b_r,  //iv_asi_oya_b_r,
        iv_asi_hito_a_r, //iv_asi_hito_a_r,
        iv_asi_hito_b_r, //iv_asi_hito_b_r,
        iv_asi_naka_a_r, //iv_asi_naka_a_r,
        iv_asi_naka_b_r, //iv_asi_naka_b_r,
        iv_asi_kusu_a_r, //iv_asi_kusu_a_r,
        iv_asi_kusu_b_r, //iv_asi_kusu_b_r,
        iv_asi_ko_a_r,   //iv_asi_ko_a_r,
        iv_asi_ko_b_r,   //iv_asi_ko_b_r,
        iv_nitoukin_l,   //iv_nitoukin_l,
        iv_nitoukin_r,   //iv_nitoukin_r,
        iv_c_mune_l,     //iv_c_mune_l,
        iv_c_mune_r,     //iv_c_mune_r,
        iv_kougan_l,     //iv_kougan_l,
        iv_kougan_r,     //iv_kougan_r,
        iv_ochinko_a,    //iv_ochinko_a,
        iv_ochinko_b,    //iv_ochinko_b,
        iv_ochinko_c,    //iv_ochinko_c,
        iv_ochinko_d,    //iv_ochinko_d,
        iv_ochinko_e,    //iv_ochinko_e,
        iv_ochinko_f,    //iv_ochinko_f,
        iv_kuritto,      //iv_kuritto,
        iv_inshin_l,     //iv_inshin_l,
        iv_inshin_r,     //iv_inshin_r,
        iv_omanko,       //iv_omanko,
        iv_koumon,       //iv_koumon,
        iv_koumon_l,     //iv_koumon_l,
        iv_koumon_r,     //iv_koumon_r,
        iv_shiri_l,      //iv_shiri_l,
        iv_shiri_r       //iv_shiri_r
    }

    public enum BoneListEn
    {
        _unknown_,
        _root_,
        e_root,
        e_abdomen,
        e_spine_a,
        e_spine_b,
        e_spine_c,
        e_breast_l,
        e_breast_r,
        e_neck,
        e_head,
        e_jaw,
        e_ear_l,
        e_ear_r,
        e_eyelid_l_l,
        e_eyelid_l_r,
        e_eyelid_u_l,
        e_eyelid_u_r,
        e_eye_l,
        e_eye_r,
        e_noes,
        e_cheek_l,
        e_cheek_r,
        e_lip_l,
        e_lip_r,
        e_eyebrow_l,
        e_eyebrow_r,
        e_bridge,
        e_brow_l,
        e_brow_r,
        e_lip_u_a,
        e_lip_l_a,
        e_lip_u_b,
        e_lip_l_b,
        e_hair_a,
        e_hair_b,
        e_hair_f_l,
        e_hair_f_r,
        e_collarbone_l,
        e_collarbone_r,
        e_waist,
        e_left_leg,
        e_right_leg,
        e_knee_l,
        e_knee_r,
        e_calf_l,
        e_calf_r,
        e_foot_l,
        e_foot_r,
        e_toes_l,
        e_toes_r,
        e_arm_l,
        e_arm_r,
        e_forearm_l,
        e_forearm_r,
        e_hand_l,
        e_hand_r,
        e_wrist_l,
        e_wrist_r,
        e_finger_index_a_l,
        e_finger_index_a_r,
        e_finger_pinky_a_l,
        e_finger_pinky_a_r,
        e_finger_ring_a_l,
        e_finger_ring_a_r,
        e_finger_middle_a_l,
        e_finger_middle_a_r,
        e_thumb_a_l,
        e_thumb_a_r,
        e_finger_index_b_l,
        e_finger_index_b_r,
        e_finger_pinky_b_l,
        e_finger_pinky_b_r,
        e_finger_ring_b_l,
        e_finger_ring_b_r,
        e_finger_middle_b_l,
        e_finger_middle_b_r,
        e_wing_a_l,
        e_wing_a_r,
        e_wing_b_l,
        e_wing_b_r,
        e_wing_c_l,
        e_wing_c_r,
        e_wing_d_l,
        e_wing_d_r,
        e_wing_e_l,
        e_wing_e_r,
        e_wing_f_l,
        e_wing_f_r,
        e_wing_g_l,
        e_wing_g_r,
        e_wing_h_l,
        e_wing_h_r,
        e_wing_i_l,
        e_wing_i_r,
        e_wing_j_l,
        e_wing_j_r,
        e_thumb_b_l,
        e_thumb_b_r,
        e_tail_a,
        e_tail_b,
        e_tail_c,
        e_tail_d,
        e_tail_e,
        e_tail1_a,
        e_tail1_b,
        e_tail1_c,
        e_tail1_d,
        e_tail1_e,
        e_tail1_l,
        e_tail1_r,
        e_load_a,
        e_load_b,
        e_load_c,
        e_load_l,
        e_load_r,
        e_throw,
        e_poleyn_l,
        e_poleyn_r,
        e_pauldron_l,
        e_pauldron_r,
        e_shoulder_l,
        e_shoulder_r,
        e_shield_l,
        e_shield_r,
        e_earing_a_l,
        e_earing_a_r,
        e_earing_b_l,
        e_earing_b_r,
        e_elbow_l,
        e_elbow_r,
        e_couter_l,
        e_couter_r,
        e_weapon_l,
        e_weapon_r,
        e_holster_l,
        e_holster_r,
        e_sheathe_l,
        e_sheathe_r,
        e_scabbard_l,
        e_scabbard_r,
        e_cloth_b_a_l,
        e_cloth_b_a_r,
        e_cloth_f_a_l,
        e_cloth_f_a_r,
        e_cloth_s_a_l,
        e_cloth_s_a_r,
        e_cloth_b_b_l,
        e_cloth_b_b_r,
        e_cloth_f_b_l,
        e_cloth_f_b_r,
        e_cloth_s_b_l,
        e_cloth_s_b_r,
        e_cloth_b_c_l,
        e_cloth_b_c_r,
        e_cloth_f_c_l,
        e_cloth_f_c_r,
        e_cloth_s_c_l,
        e_cloth_s_c_r,
        e_mount,
        e_mount_second,
        j_ex_met_a,
        j_ex_met_l,
        j_ex_met_r,
        j_ex_top_a,
        j_ex_top_c,
        j_ex_top_b,
        j_ex_top_d,
        j_zera_a_l,
        j_zera_a_r,
        j_zerb_a_l,
        j_zerb_a_r,
        j_zerc_a_l,
        j_zerc_a_r,
        j_zerd_a_l,
        j_zerd_a_r,
        j_zera_b_l,
        j_zera_b_r,
        j_zerb_b_l,
        j_zerb_b_r,
        j_zerc_b_l,
        j_zerc_b_r,
        j_zerd_b_l,
        j_zerd_b_r,
        j_zacc,
        j_f_hige_l,
        j_f_hige_r,
        j_f_uago,
        j_f_ulip,
        n_f_lip_l,
        n_f_lip_r,
        n_f_ulip_l,
        n_f_ulip_r,
        j_f_dlip,
        iv_ko_c_l,
        iv_kusu_c_l,
        iv_naka_c_l,
        iv_hito_c_l,
        iv_ko_c_r,
        iv_kusu_c_r,
        iv_naka_c_r,
        iv_hito_c_r,
        iv_asi_oya_a_l,
        iv_asi_oya_b_l,
        iv_asi_hito_a_l,
        iv_asi_hito_b_l,
        iv_asi_naka_a_l,
        iv_asi_naka_b_l,
        iv_asi_kusu_a_l,
        iv_asi_kusu_b_l,
        iv_asi_ko_a_l,
        iv_asi_ko_b_l,
        iv_asi_oya_a_r,
        iv_asi_oya_b_r,
        iv_asi_hito_a_r,
        iv_asi_hito_b_r,
        iv_asi_naka_a_r,
        iv_asi_naka_b_r,
        iv_asi_kusu_a_r,
        iv_asi_kusu_b_r,
        iv_asi_ko_a_r,
        iv_asi_ko_b_r,
        iv_nitoukin_l,
        iv_nitoukin_r,
        iv_c_mune_l,
        iv_c_mune_r,
        iv_kougan_l,
        iv_kougan_r,
        iv_ochinko_a,
        iv_ochinko_b,
        iv_ochinko_c,
        iv_ochinko_d,
        iv_ochinko_e,
        iv_ochinko_f,
        iv_kuritto,
        iv_inshin_l,
        iv_inshin_r,
        iv_omanko,
        iv_koumon,
        iv_koumon_l,
        iv_koumon_r,
        iv_shiri_l,
        iv_shiri_r
    }
}