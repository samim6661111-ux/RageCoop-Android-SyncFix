using GTA;
using GTA.Native;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace RageCoop.Client
{
    internal class Chat
    {
        private readonly Scaleform MainScaleForm;

        public string CurrentInput { get; set; }

        private bool CurrentFocused { get; set; }
        public bool Focused
        {
            get => CurrentFocused;
            set
            {
                if (value && Hidden)
                {
                    Hidden = false;
                }

                MainScaleForm.CallFunction("SET_FOCUS", value ? 2 : 1, 2, "ALL");

                CurrentFocused = value;
            }
        }

        private ulong LastMessageTime { get; set; }

        private bool CurrentHidden { get; set; }
        private bool Hidden
        {
            get => CurrentHidden;
            set
            {
                if (value)
                {
                    if (!CurrentHidden)
                    {
                        MainScaleForm.CallFunction("hide");
                    }
                }
                else if (CurrentHidden)
                {
                    MainScaleForm.CallFunction("showFeed");
                }

                CurrentHidden = value;
            }
        }

        public Chat()
        {
            MainScaleForm = new Scaleform("multiplayer_chat");
        }

        public void Init()
        {
            MainScaleForm.CallFunction("SET_FOCUS", 2, 2, "ALL");
            MainScaleForm.CallFunction("SET_FOCUS", 1, 2, "ALL");
        }

        public void Clear()
        {
            MainScaleForm.CallFunction("RESET");
        }

        public void Tick()
        {
            if ((Util.GetTickCount64() - LastMessageTime) > 15000 && !Focused && !Hidden)
            {
                Hidden = true;
            }

            if (!Hidden)
            {
                MainScaleForm.Render2D();
            }

            if (!CurrentFocused)
            {
                return;
            }

            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);
        }

        public void AddMessage(string sender, string msg)
        {
            MainScaleForm.CallFunction("ADD_MESSAGE", sender + ":", msg);
            LastMessageTime = Util.GetTickCount64();
            Hidden = false;
        }

        public void OnKeyDown(Keys key)
        {
            if (key == Keys.Escape)
            {
                Focused = false;
                CurrentInput = "";
                return;
            }

            if (key == Keys.PageUp)
            {
                MainScaleForm.CallFunction("PAGE_UP");
            }
            else if (key == Keys.PageDown)
            {
                MainScaleForm.CallFunction("PAGE_DOWN");
            }

            string keyChar = GetCharFromKey(key, Game.IsKeyPressed(Keys.ShiftKey), false);

            if (keyChar.Length == 0)
            {
                return;
            }

            switch (keyChar[0])
            {
                case (char)8:
                    if (CurrentInput?.Length > 0)
                    {
                        CurrentInput = CurrentInput.Remove(CurrentInput.Length - 1);
                        MainScaleForm.CallFunction("DELETE_TEXT");
                    }
                    return;
                case (char)13:
                    MainScaleForm.CallFunction("ADD_TEXT", "ENTER");

                    if (!string.IsNullOrWhiteSpace(CurrentInput))
                    {
                        Networking.SendChatMessage(CurrentInput);
                    }

                    Focused = false;
                    CurrentInput = "";
                    return;
                default:
                    CurrentInput += keyChar;
                    MainScaleForm.CallFunction("ADD_TEXT", keyChar);
                    return;
            }
        }

        

        public static string GetCharFromKey(Keys key, bool shift, bool altGr)
        {
            // US keyboard layout. The Win32 keyboard layout API (ToUnicodeEx) does not exist on Android.
            int k = (int)key & 0xFFFF;
            if (k >= (int)Keys.A && k <= (int)Keys.Z)
            {
                char c = (char)('a' + (k - (int)Keys.A));
                return (shift ? char.ToUpperInvariant(c) : c).ToString();
            }
            if (k >= (int)Keys.D0 && k <= (int)Keys.D9)
            {
                int d = k - (int)Keys.D0;
                return shift ? ")!@#$%^&*("[d].ToString() : ((char)('0' + d)).ToString();
            }
            if (k >= (int)Keys.NumPad0 && k <= (int)Keys.NumPad9)
                return ((char)('0' + (k - (int)Keys.NumPad0))).ToString();
            switch ((Keys)k)
            {
                case Keys.Space: return " ";
                case Keys.OemMinus: return shift ? "_" : "-";
                case Keys.Oemplus: return shift ? "+" : "=";
                case Keys.OemOpenBrackets: return shift ? "{" : "[";
                case Keys.OemCloseBrackets: return shift ? "}" : "]";
                case Keys.OemPipe: return shift ? "|" : "\\";
                case Keys.OemSemicolon: return shift ? ":" : ";";
                case Keys.OemQuotes: return shift ? "\"" : "'";
                case Keys.Oemcomma: return shift ? "<" : ",";
                case Keys.OemPeriod: return shift ? ">" : ".";
                case Keys.OemQuestion: return shift ? "?" : "/";
                case Keys.Oemtilde: return shift ? "~" : "`";
                case Keys.Multiply: return "*";
                case Keys.Divide: return "/";
                case Keys.Add: return "+";
                case Keys.Subtract: return "-";
                case Keys.Decimal: return ".";
            }
            return "";
        }
    }
}
