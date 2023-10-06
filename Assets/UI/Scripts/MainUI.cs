using Convexify;
using UnityEngine;

namespace Doji.ConvexifyDemo {

    public class MainUI : MonoBehaviour {

        public GameObject DragAndDropArea;
        public GameObject IcoInspector;
        public GameObject ConversionInspector;

        public GameObject ShowModelToggle;
        public GameObject ShowCHToggle;
        public GameObject VisualizationObject;

        public static MainUI Instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<MainUI>();
                }
                return _instance;
            }
            set { _instance = value; }
        }
        private static MainUI _instance;

        private void Awake() {
            DragAndDropArea.SetActive(true);
            VHACDProperties.Instance.gameObject.SetActive(false);
            ShowModelToggle.SetActive(false);
            ShowCHToggle.SetActive(false);
        }

        /// <summary>
        /// Called once the OBJ model is loaded
        /// </summary>
        public void ShowMainView() {
            DragAndDropArea.SetActive(false);
            VHACDProperties.Instance.gameObject.SetActive(true);
        }

        /// <summary>
        /// Called once the convex hulls are calculated
        /// </summary>
        public void OnCHCalculated() {
            VHACDProperties.Instance.gameObject.SetActive(false);
            ShowModelToggle.SetActive(true);
            ShowCHToggle.SetActive(true);
        }

        public void CalculateConvexHulls() {
            WebGLUtils.SetCursorToWaitJS();
            Invoke("DelayedCalculate", 0.1f);
        }

        private void DelayedCalculate() {
            WebGLUtils.ResetCursorJS();
            GameObject g = FileReceiver.Instance.LoadedObject;
            if (g == null) {
                return;
            }

            VHACD vhacd = new VHACD() {
                Parameters = VHACDProperties.Instance.Parameters
            };
            MeshFilter mf = g.GetComponentInChildren<MeshFilter>();
            ConvexDecomposition cd = vhacd.Compute(mf.sharedMesh, 0);
            vhacd.GenerateColliders(cd, g);
            VisualizeConvexHulls(cd, g);
            OnCHCalculated();
        }

        /// <summary>
        /// Displays the generated convexhulls by rendering them as meshes.
        /// </summary>
        private void VisualizeConvexHulls(ConvexDecomposition cd, GameObject orig) {
            VisualizationObject = new GameObject("Visualization");
            VisualizationObject.transform.position = orig.transform.position;
            VisualizationObject.transform.rotation = orig.transform.rotation;
            VisualizationObject.transform.localScale = orig.transform.localScale;
            for (int i = 0; i < cd.ConvexHulls.Count; i++) {
                var ch = cd.ConvexHulls[i];
                Mesh m = new Mesh();
                m.SetVertices(ch.Points);
                m.SetIndices(ch.Indices, MeshTopology.Triangles, 0);
                m.RecalculateNormals();
                GameObject g = new GameObject();
                g.transform.SetParent(VisualizationObject.transform, false);
                g.name = $"ConvexHull {i} ({ch.Points.Count} vertices, {ch.Indices.Count / 3} tris)";
                var mf = g.AddComponent<MeshFilter>();
                var mr = g.AddComponent<MeshRenderer>();
                mr.sharedMaterial = new Material(Shader.Find("Standard"));
                mr.sharedMaterial.color = Random.ColorHSV(0f, 1f, 0.7f, 0.7f, 0.8f, 1f);
                mf.sharedMesh = m;
            }
        }
    }
}