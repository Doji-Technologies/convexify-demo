#if UNITY_WEBGL && !UNITY_EDITOR
#define PLATFORM_SUPPORTED
#endif

using System.Runtime.InteropServices;

/// <summary>
/// Browser functions of the Web player: file picker and file download. No effect on other platforms.
/// </summary>
public static class WebGLUtils {
#if PLATFORM_SUPPORTED
    [DllImport("__Internal")]
    private static extern void ConvexifyOpenFilePicker(string gameObjectName, string methodName, string accept);

    [DllImport("__Internal")]
    private static extern void ConvexifyDownloadFile(byte[] bytes, int length, string fileName, string mimeType);
#endif

    /// <summary>True in a Web player.</summary>
    public static bool IsSupported {
        get {
#if PLATFORM_SUPPORTED
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Opens the file picker of the browser. When the user selects a file, the page sends a JSON string
    /// <c>{"url", "fileName", "size"}</c> to <paramref name="methodName"/> on <paramref name="gameObjectName"/>.
    /// </summary>
    public static void OpenFilePicker(string gameObjectName, string methodName, string accept) {
#if PLATFORM_SUPPORTED
        ConvexifyOpenFilePicker(gameObjectName, methodName, accept);
#endif
    }

    /// <summary>Makes the browser download <paramref name="bytes"/> as a file.</summary>
    public static void DownloadFile(byte[] bytes, string fileName, string mimeType = "application/octet-stream") {
#if PLATFORM_SUPPORTED
        ConvexifyDownloadFile(bytes, bytes.Length, fileName, mimeType);
#endif
    }
}
