using UnityEngine;

namespace Doji.ConvexifyDemo {
    public class CameraManager : MonoBehaviour {

        public static CameraManager Instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<CameraManager>();
                }
                return _instance;
            }
            set { _instance = value; }
        }
        private static CameraManager _instance;

        private Transform mainCamera;

        private void Awake() {
            mainCamera = Camera.main.transform;
        }

        public void OnModelLoader(GameObject g) {
            MeshRenderer r = g.GetComponentInChildren<MeshRenderer>();
            if (r != null) {
                FitCameraToBounds(r);
            }
        }

        private void FitCameraToBounds(MeshRenderer target) {
            const float minDistanceMultiplier = 2.0f; // Minimum multiplier for distance
            const float maxDistanceMultiplier = 5.0f; // Maximum multiplier for distance

            // Get the bounds of the target MeshRenderer
            Bounds targetBounds = target.bounds;

            // Calculate the desired distance based on the object's size
            float objectSize = Mathf.Max(targetBounds.size.x, targetBounds.size.y, targetBounds.size.z);
            float targetDistance = Mathf.Clamp(objectSize * 0.5f, objectSize * minDistanceMultiplier, objectSize * maxDistanceMultiplier);

            // Calculate the camera's position relative to the object
            Vector3 cameraPosition = targetBounds.center - mainCamera.forward * targetDistance;

            // Set the camera's position
            mainCamera.position = cameraPosition;

            // Rotate the camera to look at the object's center
            Quaternion targetRotation = Quaternion.LookRotation(targetBounds.center - cameraPosition);
            mainCamera.rotation = targetRotation;
        }
    }
}