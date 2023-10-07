using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Doji.ConvexifyDemo {
    public class ExportButton : MonoBehaviour {

        private Button _button;

        private void Awake() {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnExportClicked);
        }

        private void OnExportClicked() {
            GameObject g = MainUI.Instance.VisualizationObject;
            if (g == null) {
                return;
            }
            IEnumerable<Mesh> meshes = g.GetComponentsInChildren<MeshFilter>().Select(mf => mf.sharedMesh);
#if UNITY_WEBGL && ! UNITY_EDITOR
            byte[] objFile = OBJExporter.Serialize(meshes);
            WebGLUtils.WriteFile(objFile, $"{g.name}_convex_hulls.obj", MimeType.Unknown);
#else
            OBJExporter.WriteToFile(meshes, $"{g.name}_convex_hulls.obj");
#endif

        }
    }
}