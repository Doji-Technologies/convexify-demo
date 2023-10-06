using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Doji.ConvexifyDemo {
    public class FloatSlider : MonoBehaviour {

        private Slider _slider;
        private TMP_Text _valueTxt;

        private void Awake() {
            _slider = GetComponentInChildren<Slider>();
            _slider.onValueChanged.AddListener(SliderValueChanged);
            _valueTxt = transform.Find("Value").GetComponent<TMP_Text>();
            _valueTxt.text = ToString(_slider.value);
        }

        private void SliderValueChanged(float value) {
            _valueTxt.text = ToString(value);
        }

        private string ToString(float value) {
            return string.Format("{0:F1}", value);
        }
        internal void SetValue(float value) {
            _slider.value = value;
        }
    }
}