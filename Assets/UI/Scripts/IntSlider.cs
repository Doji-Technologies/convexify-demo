using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Doji.ConvexifyDemo {
    public class IntSlider : MonoBehaviour {

        private Slider _slider;
        private TMP_Text _valueTxt;

        private void Awake() {
            _slider = GetComponentInChildren<Slider>();
            _slider.onValueChanged.AddListener(SliderValueChanged);
            _valueTxt = transform.Find("Value").GetComponent<TMP_Text>();
            _valueTxt.text = ((int)_slider.value).ToString();
        }

        private void SliderValueChanged(float value) {
            _valueTxt.text = ((int)value).ToString();
        }

        internal void SetValue(int value) {
            _slider.value = value;
        }
    }
}