using Convexify;
using UnityEngine;

namespace Doji.ConvexifyDemo {

    public class VHACDProperties : MonoBehaviour {
        public static VHACDProperties Instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<VHACDProperties>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            set { _instance = value; }
        }
        private static VHACDProperties _instance;

        public Parameters Parameters { get; set; } = new Parameters();

        public IntSlider MaxConvexHulls;
        public IntSlider Resolution;
        public FloatSlider MinimumVolumePercentErrorAllowed;
        public IntSlider MaxRecursionDepth;
        public IntSlider MaxNumVerticesPerCH;

        private void Start() {
            Initialize();
        }

        private void Initialize() {
            // default values
            MaxConvexHulls                  .SetValue(Parameters.MaxConvexHulls);
            Resolution                      .SetValue(Parameters.Resolution);
            MinimumVolumePercentErrorAllowed.SetValue(Parameters.MinimumVolumePercentErrorAllowed);
            MaxRecursionDepth               .SetValue(Parameters.MaxRecursionDepth);
            MaxNumVerticesPerCH             .SetValue(Parameters.MaxNumVerticesPerCH);
        }

        public void SetMaxConvexHulls(float maxConvexHulls) {
            Parameters.MaxConvexHulls = (int)maxConvexHulls;
        }

        public void SetResolution(float resolution) {
            Parameters.Resolution = (int)resolution;
        }

        public void SetMinVolumeError(float minVolumeError) {
            Parameters.MinimumVolumePercentErrorAllowed = minVolumeError;
        }

        public void SetMaxrecursionDepth(float maxRecursionDepth) {
            Parameters.MaxRecursionDepth = (int)maxRecursionDepth;
        }

        public void SetMaxVerticesPerCH(float maxVerticesPerCH) {
            Parameters.MaxNumVerticesPerCH = (int)maxVerticesPerCH;
        }
    }
}