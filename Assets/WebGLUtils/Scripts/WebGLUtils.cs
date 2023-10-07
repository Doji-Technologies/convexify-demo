#if UNITY_WEBGL && !UNITY_EDITOR
#define PLATFORM_SUPPORTED
#endif

using System.IO;
using System.Runtime.InteropServices;

public static class WebGLUtils {
#if PLATFORM_SUPPORTED
    [DllImport("__Internal")]
    private static extern void SetCursorToWait();
    [DllImport("__Internal")]
    private static extern void ResetCursor();

    [DllImport("__Internal")]
    private static extern void DownloadFile(byte[] byteArray, int byteArrayLength, string fileName, string mimeType);
    [DllImport("__Internal")]
    private static extern void DownloadFileStream(int streamPointer, string fileName, string mimeType);
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="byteArray">The byte array to write</param>
    /// <param name="fileName">the file name including the file extension</param>
    public static void WriteFile(byte[] byteArray, string fileName, MimeType mimeType = MimeType.Unknown) {
#if PLATFORM_SUPPORTED
        string type = mimeType.ToMimeString();
        DownloadFile(byteArray, byteArray.Length, fileName, type);
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileStream">the stream to write</param>
    /// <param name="fileName">the file name including the file extension</param>
    public static void WriteFile(FileStream fileStream, string fileName, MimeType mimeType = MimeType.Unknown) {
#if PLATFORM_SUPPORTED
        string type = mimeType.ToMimeString();
        int streamPointer = fileStream.SafeFileHandle.DangerousGetHandle().ToInt32();
        DownloadFileStream(streamPointer, fileName, type);
#endif
    }
}