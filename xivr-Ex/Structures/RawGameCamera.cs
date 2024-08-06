using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace xivr.Structures;


[StructLayout(LayoutKind.Explicit)]
public unsafe struct GameCamera
{
    [FieldOffset(0x00)] public CameraBase CameraBase;
    [FieldOffset(0x10)] public RawGameCamera Camera;
}

[StructLayout(LayoutKind.Explicit)]
public struct vtblRawGameCamera
{
    [FieldOffset(0x78)] // 15*8=112
    public unsafe delegate* unmanaged[Stdcall]<RawGameCamera*, IntPtr, float*, bool, void> vf15;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct RawGameCamera
{
    [FieldOffset(0x0)] public vtblRawGameCamera* vtbl;
    [FieldOffset(0x50)] public Vector3 Position;
    [FieldOffset(0x80)] public Vector3 LookAt;
    [FieldOffset(0x90)] public float uk1a;
    [FieldOffset(0x94)] public float uk1b;
    [FieldOffset(0x98)] public float uk1c;
    [FieldOffset(0xA0)] public Matrix4x4 ViewMatrix;
    [FieldOffset(0xE0)] public CameraConstantBuffer* BufferData;
    [FieldOffset(0xE8)] public float unk2;
    [FieldOffset(0xEC)] public byte unk3;
    [FieldOffset(0x104)] public float CurrentZoom;
    [FieldOffset(0x108)] public float MinZoom;
    [FieldOffset(0x10C)] public float MaxZoom;
    [FieldOffset(0x110)] public float CurrentFoV;
    [FieldOffset(0x114)] public float MinFoV;
    [FieldOffset(0x118)] public float MaxFoV;
    [FieldOffset(0x11C)] public float AddedFoV;
    [FieldOffset(0x120)] public float CurrentHRotation;
    [FieldOffset(0x124)] public float CurrentVRotation;
    [FieldOffset(0x128)] public float HRotationThisFrame1;
    [FieldOffset(0x12C)] public float VRotationThisFrame1;
    [FieldOffset(0x130)] public float HRotationThisFrame2;
    [FieldOffset(0x134)] public float VRotationThisFrame2;
    [FieldOffset(0x138)] public float MinVRotation;
    [FieldOffset(0x13C)] public float MaxVRotation;
    [FieldOffset(0x150)] public float Tilt;
    [FieldOffset(0x160)] public CameraModes Mode; // Camera mode? (0 = 1st person, 1 = 3rd person, 2+ = weird controller mode? cant look up/down)
    [FieldOffset(0x16C)] public float InterpolatedZoom;
    [FieldOffset(0x1A0)] public float ViewX;
    [FieldOffset(0x1A4)] public float ViewY;
    [FieldOffset(0x1A8)] public float ViewZ;
    [FieldOffset(0x214)] public float LookAtHeightOffset;
    [FieldOffset(0x218)] public byte ResetLookatHeightOffset;
    [FieldOffset(0x2A4)] public float LookAtZ2;

}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct CameraConstantBuffer
{
    [FieldOffset(0x0)] public UInt64 vtbl;
    [FieldOffset(0x10)] public Matrix4x4 ViewMatrix;
    [FieldOffset(0x50)] public Matrix4x4 ProjectionMatrix;
    [FieldOffset(0x90)] public float EyePosX;
    [FieldOffset(0x94)] public float EyePosY;
    [FieldOffset(0x98)] public float EyePosZ;
    [FieldOffset(0xA0)] public Matrix4x4 ViewMatrix1;
    [FieldOffset(0xE0)] public Matrix4x4 ProjectionMatrix1;
    [FieldOffset(0x120)] public Matrix4x4 ViewMatrix2;
    [FieldOffset(0x160)] public Matrix4x4 ProjectionMatrix2;
    [FieldOffset(0x1A0)] public Matrix4x4 ProjectionText;
    [FieldOffset(0x1E8)] public float MkProj1;
    [FieldOffset(0x1EC)] public float Aspect;
    [FieldOffset(0x1F0)] public float NearClip;
    [FieldOffset(0x1F4)] public float FarClip;
}
