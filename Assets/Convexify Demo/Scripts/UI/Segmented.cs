using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Doji.ConvexifyDemo {

    /// <summary>
    /// A labeled segmented control: one button for each choice, one selected at a time.
    /// </summary>
    [UxmlElement]
    public partial class Segmented : VisualElement, INotifyValueChanged<int> {

        [UxmlAttribute]
        public string Label {
            get => _label.text;
            set => _label.text = value;
        }

        /// <summary>The choices, separated by commas.</summary>
        [UxmlAttribute]
        public string Choices {
            get => _choices;
            set { _choices = value; Rebuild(); }
        }

        /// <summary>A short explanation, shown in the hint area while the pointer is over the control.</summary>
        [UxmlAttribute]
        public string Hint { get; set; }

        public int value {
            get => _value;
            set {
                if (_value == value) {
                    return;
                }
                using ChangeEvent<int> evt = ChangeEvent<int>.GetPooled(_value, value);
                evt.target = this;
                SetValueWithoutNotify(value);
                SendEvent(evt);
            }
        }

        private readonly Label _label;
        private readonly VisualElement _row;
        private readonly List<Button> _items = new List<Button>();
        private string _choices = "";
        private int _value;

        public Segmented() {
            AddToClassList("segmented");
            _label = new Label();
            _label.AddToClassList("segmented__label");
            _row = new VisualElement();
            _row.AddToClassList("segmented__row");
            Add(_label);
            Add(_row);
        }

        public void SetValueWithoutNotify(int newValue) {
            _value = newValue;
            for (int i = 0; i < _items.Count; i++) {
                _items[i].EnableInClassList("segmented__item--selected", i == newValue);
            }
        }

        private void Rebuild() {
            _row.Clear();
            _items.Clear();
            string[] parts = _choices.Split(',');
            for (int i = 0; i < parts.Length; i++) {
                int index = i;
                var item = new Button(() => value = index) { text = parts[i].Trim() };
                item.AddToClassList("segmented__item");
                _row.Add(item);
                _items.Add(item);
            }
            SetValueWithoutNotify(_value);
        }
    }
}
