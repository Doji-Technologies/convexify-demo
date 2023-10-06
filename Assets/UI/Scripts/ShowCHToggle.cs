using UnityEngine;
using UnityEngine.UI;

namespace Doji.ConvexifyDemo {

    public class ShowCHToggle : MonoBehaviour {

        private Toggle _toggle;

        private void Start() {
            _toggle = GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener(OnShowModelToggled);
        }

        private void OnShowModelToggled(bool value) {
            GameObject obj = MainUI.Instance.VisualizationObject;
            if (obj == null) {
                return;
            }
            obj.SetActive(value);
        }
    }
}