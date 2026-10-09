using UnityEngine;

namespace Doji.ConvexifyDemo {

    /// <summary>
    /// Orbits the camera around a pivot with smoothing. The UI viewport element feeds it pointer and wheel input.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OrbitCamera : MonoBehaviour {

        public float RotateSpeed = 0.3f;
        public float Damping = 14f;

        /// <summary>
        /// Moves the image center in normalized device coordinates, so the pivot stays in the middle of the
        /// screen area that the UI does not cover.
        /// </summary>
        public Vector2 ScreenShift { get; set; }

        private Camera _camera;
        private Vector3 _pivot, _targetPivot;
        private float _yaw = -35f, _targetYaw = -35f;
        private float _pitch = 22f, _targetPitch = 22f;
        private float _distance = 5f, _targetDistance = 5f;
        private float _radius = 1f;
        private float _fitDistance = 5f;

        private void Awake() {
            _camera = GetComponent<Camera>();
        }

        /// <summary>Frames <paramref name="bounds"/> from the default angle.</summary>
        public void Frame(Bounds bounds, bool instant) {
            Frame(bounds, instant, keepAngles: false);
        }

        /// <summary>Frames <paramref name="bounds"/>. With <paramref name="keepAngles"/>, only the pivot and the distance change.</summary>
        public void Frame(Bounds bounds, bool instant, bool keepAngles) {
            _radius = Mathf.Max(bounds.extents.magnitude, 1e-4f);
            _targetPivot = bounds.center;
            if (!keepAngles) {
                _targetYaw = -35f;
                _targetPitch = 22f;
            }
            // fit the bounding sphere into the free screen area, vertically and horizontally
            float tanHalf = Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanVertical = tanHalf * (1f - Mathf.Abs(ScreenShift.y));
            float tanHorizontal = tanHalf * _camera.aspect * (1f - Mathf.Abs(ScreenShift.x));
            float halfAngle = Mathf.Atan(Mathf.Min(tanVertical, tanHorizontal));
            _targetDistance = _radius / Mathf.Sin(halfAngle) * 0.9f;
            _fitDistance = _targetDistance;
            if (instant) {
                _pivot = _targetPivot;
                _yaw = _targetYaw;
                _pitch = _targetPitch;
                _distance = _targetDistance;
                Apply();
            }
        }

        /// <summary>Turns the view around the vertical axis by <paramref name="degrees"/>.</summary>
        public void AddYaw(float degrees) {
            _targetYaw += degrees;
        }

        /// <summary>Sets the view angles and the distance as a multiple of the framed distance.</summary>
        public void SetView(float yaw, float pitch, float zoom, bool instant) {
            _targetYaw = yaw;
            _targetPitch = pitch;
            _targetDistance = _fitDistance * zoom;
            if (instant) {
                _yaw = _targetYaw;
                _pitch = _targetPitch;
                _distance = _targetDistance;
            }
        }

        public void Rotate(Vector2 deltaPixels) {
            _targetYaw += deltaPixels.x * RotateSpeed;
            _targetPitch = Mathf.Clamp(_targetPitch + deltaPixels.y * RotateSpeed, -89f, 89f);
        }

        public void Pan(Vector2 deltaPixels) {
            // world units per pixel at the pivot distance
            float scale = 2f * _distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Max(Screen.height, 1);
            Quaternion rotation = Quaternion.Euler(_targetPitch, _targetYaw, 0);
            _targetPivot += rotation * new Vector3(-deltaPixels.x, deltaPixels.y, 0) * scale;
        }

        /// <summary>Scales the distance by e^<paramref name="delta"/>.</summary>
        public void Zoom(float delta) {
            _targetDistance = Mathf.Clamp(_targetDistance * Mathf.Exp(delta), _radius * 0.2f, _radius * 40f);
        }

        private void LateUpdate() {
            float t = 1f - Mathf.Exp(-Damping * Time.unscaledDeltaTime);
            _pivot = Vector3.Lerp(_pivot, _targetPivot, t);
            _yaw = Mathf.Lerp(_yaw, _targetYaw, t);
            _pitch = Mathf.Lerp(_pitch, _targetPitch, t);
            _distance = Mathf.Lerp(_distance, _targetDistance, t);
            Apply();
        }

        private void Apply() {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            transform.SetPositionAndRotation(_pivot - rotation * Vector3.forward * _distance, rotation);
            _camera.nearClipPlane = Mathf.Max(_distance - _radius * 4f, _distance * 0.01f);
            _camera.farClipPlane = _distance + _radius * 8f;
            _camera.ResetProjectionMatrix();
            if (ScreenShift != Vector2.zero) {
                // x_ndc = m00 * x / -z - m02, so m02 = -shift moves the image by +shift
                Matrix4x4 projection = _camera.projectionMatrix;
                projection.m02 = -ScreenShift.x;
                projection.m12 = -ScreenShift.y;
                _camera.projectionMatrix = projection;
            }
        }
    }
}
