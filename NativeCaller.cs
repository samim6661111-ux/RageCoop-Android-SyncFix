using GTA.Math;
using GTA.Native;
using System;
using System.Runtime.InteropServices;

namespace RageCoop.Client
{
    [StructLayout(LayoutKind.Explicit, Size = 80)]
    public struct HeadBlendData
    {
        [FieldOffset(0)]
        public int ShapeFirst;

        [FieldOffset(8)]
        public int ShapeSecond;

        [FieldOffset(16)]
        public int ShapeThird;

        [FieldOffset(24)]
        public int SkinFirst;

        [FieldOffset(32)]
        public int SkinSecond;

        [FieldOffset(40)]
        public int SkinThird;

        [FieldOffset(48)]
        public float ShapeMix;

        [FieldOffset(56)]
        public float SkinMix;

        [FieldOffset(64)]
        public float ThirdMix;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct NativeVector3
    {
        [FieldOffset(0)]
        public float X;

        [FieldOffset(8)]
        public float Y;

        [FieldOffset(16)]
        public float Z;

        public static implicit operator Vector3(NativeVector3 vec)
        {
            return new Vector3() { X = vec.X, Y = vec.Y, Z = vec.Z };
        }
        public static implicit operator NativeVector3(Vector3 vec)
        {
            return new NativeVector3() { X = vec.X, Y = vec.Y, Z = vec.Z };
        }
    }
    public static class NativeCaller
    {
        // The original imported ScriptHookV.dll with mangled C++ names and called natives from whatever thread it was on.
        // Here every call goes through SHVDN.NativeFunc, which hands the call to the game thread (natives are not
        // thread safe in the arm64 port) and ends up in libgtavbridge.so (bridge_nativeInit/Push64/Call).
        public static unsafe R Invoke<R>(ulong hash) where R : struct
        {
            ulong* res = SHVDN.NativeFunc.Invoke(hash, new ulong[0]);
            return ReadResult<R>(res);
        }

        public static unsafe R Invoke<R>(Hash hash, params object[] args) where R : struct
        {
            ulong* res = SHVDN.NativeFunc.Invoke((ulong)hash, args);
            return ReadResult<R>(res);
        }

        private static unsafe R ReadResult<R>(ulong* res) where R : struct
        {
            if (res == null)
                return default(R);
            return (R)Marshal.PtrToStructure(new IntPtr(res), typeof(R));
        }
    }

}
