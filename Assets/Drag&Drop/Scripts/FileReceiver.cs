using Convexify;
using Dummiesman;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Doji.ConvexifyDemo {

    public class FileReceiver : MonoBehaviour {

#if UNITY_EDITOR
        private void Awake() {
            StartCoroutine(LoadOBJFile(@"file:\\\sample.obj"));
        }
#endif

        private IEnumerator LoadOBJFile(string url) {
            using UnityWebRequest wr = UnityWebRequest.Get(url);
            wr.SendWebRequest();
            while (!wr.isDone) {
                yield return null;
            }


            byte[] data = wr.downloadHandler.data;
            OBJLoader objLoader = new OBJLoader();
            using MemoryStream stream = new MemoryStream(data);
            GameObject g = objLoader.Load(stream);
            MainUI.Instance.ShowMainView();
            CameraManager.Instance.OnModelLoader(g);
            VHACD vhacd = new VHACD();
            MeshFilter mf = g.GetComponentInChildren<MeshFilter>();
            if (mf == null) {
                yield break;
            }

            ConvexDecomposition cd = vhacd.Compute(mf.sharedMesh, 0);
            vhacd.GenerateColliders(cd, g);
            VisualizeConvexHulls(cd, g);
        }

        /// <summary>
        /// Displays the generated convexhulls by rendering them as meshes.
        /// </summary>
        private void VisualizeConvexHulls(ConvexDecomposition cd, GameObject orig) {
            GameObject visObj = new GameObject("Visualization");
            visObj.transform.position = orig.transform.position;
            visObj.transform.rotation = orig.transform.rotation;
            visObj.transform.localScale = orig.transform.localScale;
            for (int i = 0; i < cd.ConvexHulls.Count; i++) {
                var ch = cd.ConvexHulls[i];
                Mesh m = new Mesh();
                m.SetVertices(ch.Points);
                m.SetIndices(ch.Indices, MeshTopology.Triangles, 0);
                m.RecalculateNormals();
                GameObject g = new GameObject();
                g.transform.SetParent(visObj.transform, false);
                g.name = $"ConvexHull {i} ({ch.Points.Count} vertices, {ch.Indices.Count / 3} tris)";
                var mf = g.AddComponent<MeshFilter>();
                var mr = g.AddComponent<MeshRenderer>();
                mr.sharedMaterial = new Material(Shader.Find("Standard"));
                mr.sharedMaterial.color = Random.ColorHSV(0f, 1f, 0.7f, 0.7f, 0.8f, 1f);
                mf.sharedMesh = m;
            }
        }
        public void OBJSelected(string url) {
            StartCoroutine(LoadOBJFile(url));
        }


    }
}