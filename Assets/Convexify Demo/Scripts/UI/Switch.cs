using UnityEngine.UIElements;

namespace Doji.ConvexifyDemo {

    /// <summary>
    /// A labeled on/off switch with an animated knob.
    /// </summary>
    [UxmlElement]
    public partial class Switch : VisualElement, INotifyValueChanged<bool> {

        [UxmlAttribute]
        public string Label {
            get => _label.text;
            set => _label.text = value;
        }

        /// <summary>A short explanation, shown in the hint area while the pointer is over the control.</summary>
        [UxmlAttribute]
        public string Hint { get; set; }

        [UxmlAttribute]
        public bool value {
            get => _value;
            set {
                if (_value == value) {
                    return;
                }
                using ChangeEvent<bool> evt = ChangeEvent<bool>.GetPooled(_value, value);
                evt.target = this;
                SetValueWithoutNotify(value);
                SendEvent(evt);
            }
        }

        private readonly Label _label;
        private bool _value;

        public Switch() {
            AddToClassList("switch");
            _label = new Label();
            _label.AddToClassList("switch__label");
            var track = new VisualElement();
            track.AddToClassList("switch__track");
            var knob = new VisualElement();
            knob.AddToClassList("switch__knob");
            track.Add(knob);
            Add(_label);
            Add(track);
            this.AddManipulator(new Clickable(() => value = !value));
        }

        public void SetValueWithoutNotify(bool newValue) {
            _value = newValue;
            EnableInClassList("switch--on", newValue);
        }
    }
}
