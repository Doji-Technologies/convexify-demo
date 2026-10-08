using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Doji.ConvexifyDemo {

    /// <summary>
    /// A labeled slider with a value readout and an accent fill. Supports integer and logarithmic ranges.
    /// </summary>
    [UxmlElement]
    public partial class ParamSlider : VisualElement {

        /// <summary>Raised when the user changes the value.</summary>
        public event Action<float> ValueChanged;

        [UxmlAttribute]
        public string Label {
            get => _label.text;
            set => _label.text = value;
        }

        [UxmlAttribute]
        public float Low {
            get => _low;
            set { _low = value; ApplyRange(); }
        }

        [UxmlAttribute]
        public float High {
            get => _high;
            set { _high = value; ApplyRange(); }
        }

        [UxmlAttribute]
        public bool Integer {
            get => _integer;
            set { _integer = value; Value = _value; }
        }

        /// <summary>Maps the slider position to the value on a log10 scale. <see cref="Low"/> must be positive.</summary>
        [UxmlAttribute]
        public bool Logarithmic {
            get => _logarithmic;
            set { _logarithmic = value; ApplyRange(); }
        }

        /// <summary>The numeric format of the readout, for example "0.00".</summary>
        [UxmlAttribute]
        public string Format {
            get => _format;
            set { _format = value; UpdateReadout(); }
        }

        [UxmlAttribute]
        public string Suffix {
            get => _suffix;
            set { _suffix = value; UpdateReadout(); }
        }

        /// <summary>A short explanation, shown in the hint area while the pointer is over the control.</summary>
        [UxmlAttribute]
        public string Hint { get; set; }

        public float Value {
            get => _value;
            set {
                _value = Quantize(Mathf.Clamp(value, Mathf.Min(_low, _high), Mathf.Max(_low, _high)));
                _slider.SetValueWithoutNotify(ToSlider(_value));
                UpdateReadout();
            }
        }

        private readonly Label _label;
        private readonly Label _readout;
        private readonly Slider _slider;
        private readonly VisualElement _fill;
        private float _low = 0f, _high = 1f, _value;
        private bool _integer, _logarithmic;
        private string _format = "0.##", _suffix = "";

        public ParamSlider() {
            AddToClassList("param");

            var header = new VisualElement();
            header.AddToClassList("param__header");
            _label = new Label();
            _label.AddToClassList("param__label");
            _readout = new Label();
            _readout.AddToClassList("param__value");
            header.Add(_label);
            header.Add(_readout);
            Add(header);

            _slider = new Slider(0f, 1f);
            _slider.AddToClassList("param__slider");
            _slider.RegisterValueChangedCallback(OnSliderChanged);
            Add(_slider);

            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.AddToClassList("param__fill");
            _slider.Q(className: BaseSlider<float>.trackerUssClassName)?.Add(_fill);

            ApplyRange();
        }

        /// <summary>Sets the value without raising <see cref="ValueChanged"/>.</summary>
        public void SetValueWithoutNotify(float value) {
            Value = value;
        }

        private void OnSliderChanged(ChangeEvent<float> evt) {
            float v = Quantize(FromSlider(evt.newValue));
            bool changed = !Mathf.Approximately(v, _value);
            _value = v;
            UpdateReadout();
            if (changed) {
                ValueChanged?.Invoke(_value);
            }
        }

        private void ApplyRange() {
            _slider.lowValue = ToSlider(_low);
            _slider.highValue = ToSlider(_high);
            Value = _value;
        }

        private float ToSlider(float v) => _logarithmic ? Mathf.Log10(Mathf.Max(v, 1e-6f)) : v;

        private float FromSlider(float s) => _logarithmic ? Mathf.Pow(10f, s) : s;

        private float Quantize(float v) {
            if (_integer) {
                return Mathf.Round(v);
            }
            // keep the readout precision for log values, so the value matches the text
            return _logarithmic ? float.Parse(v.ToString("G3", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) : v;
        }

        private void UpdateReadout() {
            string text = _integer ? ((int)_value).ToString(CultureInfo.InvariantCulture)
                                   : _value.ToString(_format, CultureInfo.InvariantCulture);
            _readout.text = text + _suffix;
            float lo = _slider.lowValue, hi = _slider.highValue;
            float t = Mathf.Approximately(hi, lo) ? 0f : Mathf.InverseLerp(lo, hi, ToSlider(_value));
            _fill.style.width = Length.Percent(t * 100f);
        }
    }
}
