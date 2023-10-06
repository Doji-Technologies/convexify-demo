#if UNITY_WEBGL && !UNITY_EDITOR
#define PLATFORM_SUPPORTED
#endif

using System.Runtime.InteropServices;

public static class WebGLUtils {
#if PLATFORM_SUPPORTED
    [DllImport("__Internal")]
    private static extern void SetCursorToWait();
    [DllImport("__Internal")]
    private static extern void ResetCursor();
#endif

    public static void SetCursorToWaitJS() {
#if PLATFORM_SUPPORTED
        SetCursorToWait();
#endif
    }

    public static void ResetCursorJS() {
#if PLATFORM_SUPPORTED
        ResetCursor();
#endif
    }
}