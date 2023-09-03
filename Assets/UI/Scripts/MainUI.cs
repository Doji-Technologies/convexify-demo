using System;
using UnityEngine;

namespace Doji.ConvexifyDemo {

    public class MainUI : MonoBehaviour {

        public GameObject DragAndDropArea;
        public GameObject IcoInspector;
        public GameObject ConversionInspector;

        public static MainUI Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<MainUI>();
                }
                return _instance;
            }
            set { _instance = value; }
        }
        private static MainUI _instance;

        public void ShowMainView() {
            DragAndDropArea.gameObject.SetActive(false);
        }
    }
}