using UnityEngine;
using UnityEngine.UI;

namespace Doji.ConvexifyDemo {
    public class SaveButton : MonoBehaviour {

        private Button _button;

        private void Start() {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnSaveButtonClicked);
        }

        private void OnSaveButtonClicked() {

        }
    }
}