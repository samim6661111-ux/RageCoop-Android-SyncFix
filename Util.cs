using GTA;
using GTA.Math;
using GTA.Native;
using RageCoop.Core;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;

[assembly: InternalsVisibleTo("RageCoop.Client.Installer")]
namespace RageCoop.Client
{
    internal static class Util
    {
        public static SizeF ResolutionMaintainRatio
        {
            get
            {
                // Get the game width and height
                int screenw = GTA.UI.Screen.Resolution.Width;
                int screenh = GTA.UI.Screen.Resolution.Height;
                // Calculate the ratio
                float ratio = (float)screenw / screenh;
                // And the width with that ratio
                float width = 1080f * ratio;
                // Finally, return a SizeF
                return new SizeF(width, 1080f);
            }
        }
        public static bool WorldToScreen(Vector3 pos, ref Point screenPos)
        {
            float x, y;
            unsafe
            {
                var res = ResolutionMaintainRatio;
                if (Function.Call<bool>(Hash.GET_SCREEN_COORD_FROM_WORLD_COORD, pos.X, pos.Y, pos.Z, &x, &y))
                {
                    screenPos = new Point((int)(res.Width * x), (int)(y * 1080));
                    return true;
                }
            }
            return false;
        }


        #region -- POINTER --
        private static int _steeringAngleOffset { get; set; }

        public static unsafe void NativeMemory()
        {
            IntPtr address;

            address = Game.FindPattern("\x74\x0A\xF3\x0F\x11\xB3\x1C\x09\x00\x00\xEB\x25", "xxxxxx????xx");
            if (address != IntPtr.Zero)
            {
                _steeringAngleOffset = *(int*)(address + 6) + 8;
            }

        }

        public static unsafe void CustomSteeringAngle(this Vehicle veh, float value)
        {
            IntPtr address = new IntPtr((long)veh.MemoryAddress);
            if (address == IntPtr.Zero || _steeringAngleOffset == 0)
            {
                return;
            }

            *(float*)(address + _steeringAngleOffset).ToPointer() = value;
        }
        #endregion
        #region MATH
        public static Vector3 LinearVectorLerp(Vector3 start, Vector3 end, ulong currentTime, int duration)
        {
            return new Vector3()
            {
                X = LinearFloatLerp(start.X, end.X, currentTime, duration),
                Y = LinearFloatLerp(start.Y, end.Y, currentTime, duration),
                Z = LinearFloatLerp(start.Z, end.Z, currentTime, duration),
            };
        }

        public static float LinearFloatLerp(float start, float end, ulong currentTime, int duration)
        {
            return (end - start) * currentTime / duration + start;
        }

        public static float Lerp(float from, float to, float fAlpha)
        {
            return (from * (1.0f - fAlpha)) + (to * fAlpha); //from + (to - from) * fAlpha
        }

        public static Vector3 RotationToDirection(Vector3 rotation)
        {
            double z = MathExtensions.DegToRad(rotation.Z);
            double x = MathExtensions.DegToRad(rotation.X);
            double num = Math.Abs(Math.Cos(x));

            return new Vector3
            {
                X = (float)(-Math.Sin(z) * num),
                Y = (float)(Math.Cos(z) * num),
                Z = (float)Math.Sin(x)
            };
        }


        #endregion
        public static string SettingsPath = AndroidPaths.DataDirectory + "/RageCoop.Client.Settings.xml";
        public static Settings ReadSettings(string path = null)
        {
            path = path ?? SettingsPath;
            XmlSerializer ser = new XmlSerializer(typeof(Settings));

            Directory.CreateDirectory(Directory.GetParent(path).FullName);
            Settings settings = null;

            if (File.Exists(path))
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    settings = (Settings)ser.Deserialize(stream);
                    settings.DataDirectory = AndroidPaths.Normalize(settings.DataDirectory);
                }
            }
            else
            {
                using (FileStream stream = File.OpenWrite(path))
                {
                    ser.Serialize(stream, settings = new Settings());
                }
            }

            return settings;
        }
        public static bool SaveSettings(string path = null, Settings settings = null)
        {
            try
            {
                path = path ?? SettingsPath;
                settings = settings ?? Main.Settings;
                Directory.CreateDirectory(Directory.GetParent(path).FullName);

                using (FileStream stream = new FileStream(path, File.Exists(path) ? FileMode.Truncate : FileMode.Create, FileAccess.ReadWrite))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(Settings));
                    ser.Serialize(stream, settings);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                // GTA.UI.Notification.Show("Error saving player settings: " + ex.Message);
            }
        }


        public static Vehicle CreateVehicle(Model model, Vector3 position, float heading = 0f)
        {
            if (!model.IsLoaded) { return null; }
            return (Vehicle)Entity.FromHandle(Function.Call<int>(Hash.CREATE_VEHICLE, model.Hash, position.X, position.Y, position.Z, heading, false, false));
        }
        public static Ped CreatePed(Model model, Vector3 position, float heading = 0f)
        {
            if (!model.IsLoaded) { return null; }
            return (Ped)Entity.FromHandle(Function.Call<int>(Hash.CREATE_PED, 26, model.Hash, position.X, position.Y, position.Z, heading, false, false));
        }
        public static void SetOnFire(this Entity e, bool toggle)
        {
            if (toggle)
            {
                Function.Call(Hash.START_ENTITY_FIRE, e.Handle);
            }
            else
            {
                Function.Call(Hash.STOP_ENTITY_FIRE, e.Handle);
            }
        }
        public static void SetFrozen(this Entity e, bool toggle)
        {
            Function.Call(Hash.FREEZE_ENTITY_POSITION, e, toggle);
        }

        public static SyncedPed GetSyncEntity(this Ped p)
        {
            if (p == null) { return null; }
            var c = EntityPool.GetPedByHandle(p.Handle);
            if (c == null) { EntityPool.Add(c = new SyncedPed(p)); }
            return c;
        }

        public static SyncedVehicle GetSyncEntity(this Vehicle veh)
        {
            if (veh == null) { return null; }
            var v = EntityPool.GetVehicleByHandle(veh.Handle);
            if (v == null) { EntityPool.Add(v = new SyncedVehicle(veh)); }
            return v;
        }

        public static void ApplyForce(this Entity e, int boneIndex, Vector3 direction, Vector3 rotation = default(Vector3), ForceType forceType = ForceType.MaxForceRot2)
        {
            Function.Call(Hash.APPLY_FORCE_TO_ENTITY, e.Handle, forceType, direction.X, direction.Y, direction.Z, rotation.X, rotation.Y, rotation.Z, boneIndex, false, true, true, false, true);
        }
        public static byte GetPlayerRadioIndex()
        {
            return (byte)Function.Call<int>(Hash.GET_PLAYER_RADIO_STATION_INDEX);
        }
        public static void SetPlayerRadioIndex(int index)
        {
            Function.Call(Hash.SET_RADIO_TO_STATION_INDEX, index);
        }

        #region WIN32

        private const UInt32 WM_KEYDOWN = 0x0100;
        public static void Reload()
        {
            // The PC client posted SHVDN's reload key to the game window (user32 PostMessage). That does not exist on
            // Android. Client resources are stopped by Resources.StopAll(); their assemblies stay loaded until the
            // next game start.
            Main.Logger?.Info("Util.Reload(): skipped on Android");
        }

        


        private static readonly System.Diagnostics.Stopwatch _tickWatch = System.Diagnostics.Stopwatch.StartNew();
        // kernel32 GetTickCount64 replacement: monotonic milliseconds, only differences are ever used
        public static ulong GetTickCount64() { return (ulong)_tickWatch.ElapsedMilliseconds; }
        #endregion
    }
}
