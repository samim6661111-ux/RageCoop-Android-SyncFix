using GTA;
using GTA.Math;
using RageCoop.Core;
using SHVDN;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RageCoop.Client
{
    internal unsafe class MemPatch
    {
        private readonly byte[] _data;
        private readonly byte[] _orginal;
        private readonly IntPtr _address;

        public MemPatch(byte* address, byte[] data)
        {
            _data = data;
            _orginal = new byte[data.Length];
            _address = (IntPtr)address;
            Marshal.Copy((IntPtr)address, _orginal, 0, data.Length);
        }

        public void Install() { Marshal.Copy(_data, 0, _address, _data.Length); }
        public void Uninstall() { Marshal.Copy(_orginal, 0, _address, _orginal.Length); }
    }

    internal static unsafe class Memory
    {
        // The weapon/radio wheel slow-mo patch scanned the Windows executable for x86 code. There is nothing to
        // scan on Android, so the patches stay null and ApplyPatches/RestorePatches are no-ops.
        public static MemPatch VignettingPatch;
        public static MemPatch VignettingCallPatch;
        public static MemPatch TimeScalePatch;

        public static void ApplyPatches()
        {
            if (VignettingPatch != null) VignettingPatch.Install();
            if (VignettingCallPatch != null) VignettingCallPatch.Install();
            if (TimeScalePatch != null) TimeScalePatch.Install();
        }

        public static void RestorePatches()
        {
            if (VignettingPatch != null) VignettingPatch.Uninstall();
            if (VignettingCallPatch != null) VignettingCallPatch.Uninstall();
            if (TimeScalePatch != null) TimeScalePatch.Uninstall();
        }

        #region OFFSET-CONST (PC build offsets, only used if direct memory reads are enabled)
        public const int PositionOffset = 144;
        public const int VelocityOffset = 800;
        public const int MatrixOffset = 96;
        #endregion

        // Direct entity-memory reads need the entity pool of libgtav.so (getScriptHandleBaseAddress) and PC offsets
        // that are unverified for the arm64 build, so the safe default is: always use the natives.
        public static bool UseDirectMemory = false;

        private static bool Direct(Entity e) { return UseDirectMemory && e.MemoryAddress != IntPtr.Zero; }

        public static Vector3 ReadPosition(this Entity e)
        {
            return Direct(e) ? ReadVector3(e.MemoryAddress + PositionOffset) : e.Position;
        }

        public static Quaternion ReadQuaternion(this Entity e)
        {
            return Direct(e) ? Quaternion.RotationMatrix(e.Matrix) : e.Quaternion;
        }

        public static Vector3 ReadRotation(this Entity e) => e.ReadQuaternion().ToEulerDegrees();

        public static Vector3 ReadVelocity(this Ped e)
        {
            return Direct(e) ? ReadVector3(e.MemoryAddress + VelocityOffset) : e.Velocity;
        }

        public static Vector3 ReadVector3(IntPtr address)
        {
            if (address == IntPtr.Zero)
                return new Vector3();
            float* ptr = (float*)address.ToPointer();
            return new Vector3() { X = *ptr, Y = ptr[1], Z = ptr[2] };
        }

        public static List<int> FindOffset(float toSearch, IntPtr start, int range = 1000, float tolerance = 0.01f)
        {
            var foundOffsets = new List<int>(100);
            for (int i = 0; i <= range; i++)
            {
                var val = NativeMemory.ReadFloat(start + i);
                if (Math.Abs(val - toSearch) < tolerance)
                {
                    foundOffsets.Add(i);
                }
            }
            return foundOffsets;
        }
    }
}
