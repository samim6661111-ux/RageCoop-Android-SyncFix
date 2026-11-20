// Voice chat used NAudio (WinMM/WASAPI), which does not exist on Android. Same surface, no audio.
// WasInitialized() stays false so the settings/menu code never tries to record.
namespace RageCoop.Client
{
    internal static class Voice
    {
        public static bool WasInitialized() => false;
        public static bool IsRecording() => false;
        public static void ClearAll() { }
        public static void StopRecording() { }
        public static void Init() { }
        public static void StartRecording() { }
        public static void AddVoiceData(byte[] buffer, int recorded) { }
    }
}
