#if UNITY_WEBGL
using System.Runtime.InteropServices;
using UnityEngine;

namespace Doji.ConvexifyDemo {

    public static class WebGLSaveAs {
        [DllImport("__Internal")]
        private static extern void DownloadFile(byte[] array, int byteLength, string fileName, string mimeType);

        public static void SaveImageFile(Texture2D texture) {
            byte[] textureBytes = texture.EncodeToPNG();
            DownloadFile(textureBytes, textureBytes.Length, $"{texture.name}.png", "image/png");
        }

        public static void SaveIcoFile(byte[] iconData, string name) {
            DownloadFile(iconData, iconData.Length, $"{name}.ico", "image/x-icon");
        }
    }
}
#endif