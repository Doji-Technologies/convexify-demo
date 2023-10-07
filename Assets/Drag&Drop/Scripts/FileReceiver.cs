using Dummiesman;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Doji.ConvexifyDemo {

    public class FileReceiver : MonoBehaviour {
        public static FileReceiver Instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<FileReceiver>();
                }
                return _instance;
            }
            set { _instance = value; }
        }
        private static FileReceiver _instance;

        public GameObject LoadedObject;

#if UNITY_EDITOR
        private void Awake() {
            StartCoroutine(LoadOBJFile(@"file:\\\sample.obj", "sample.obj"));
        }
#endif

        private IEnumerator LoadOBJFile(string url, string fileName) {
            using UnityWebRequest wr = UnityWebRequest.Get(url);
            wr.SendWebRequest();
            while (!wr.isDone) {
                yield return null;
            }

            byte[] data = wr.downloadHandler.data;
            OBJLoader objLoader = new OBJLoader();
            using MemoryStream stream = new MemoryStream(data);
            LoadedObject = objLoader.Load(stream);
            LoadedObject.name = fileName;
            MainUI.Instance.ShowMainView();
            CameraManager.Instance.OnModelLoaded(LoadedObject);
        }

        public void OBJSelected(string fileInfoJson) {
            FileInfo fileInfo = JsonUtility.FromJson<FileInfo>(fileInfoJson);
            StartCoroutine(LoadOBJFile(fileInfo.url, Path.GetFileNameWithoutExtension(fileInfo.fileName)));
        }


        [System.Serializable]
        public class FileInfo {
            public string url;
            public string fileName;
            public string size;
        }
    }
}