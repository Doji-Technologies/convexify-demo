using UnityEngine;

namespace Doji.ConvexifyDemo {

    [RequireComponent(typeof(Camera))]
    public class MouseLook : MonoBehaviour {

        private float mainSpeed = 20.0f; //regular speed
        private float shiftAdd = 250.0f; //multiplied by how long shift is held.  Basically running
        private float maxShift = 1000.0f; //Maximum speed when holdin gshift
        private float camSens = 0.25f; //How sensitive it with mouse
        private Vector3 lastMouse = new Vector3(255, 255, 255); //kind of in the middle of the screen, rather than at the top (play)
        private float totalRun = 1.0f;

        void Update() {
            if (Input.GetMouseButton(1)) {
                lastMouse = Input.mousePosition - lastMouse;
                lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0);
                lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x, transform.eulerAngles.y + lastMouse.y, 0);
                transform.eulerAngles = lastMouse;
            }
            lastMouse = Input.mousePosition;

            //Keyboard commands
            float f = 0.0f;
            Vector3 p = GetBaseInput();
            if (p.sqrMagnitude > 0) { // only move while a direction key is pressed
                if (Input.GetKey(KeyCode.LeftShift)) {
                    totalRun += Time.deltaTime;
                    p = p * totalRun * shiftAdd;
                    p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
                    p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
                    p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
                } else {
                    totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
                    p = p * mainSpeed;
                }

                p = p * Time.deltaTime;
                transform.Translate(p);
            }
        }

        private Vector3 GetBaseInput() { //returns the basic values, if it's 0 than it's not active.
            Vector3 p_Velocity = new Vector3();
            if (Input.GetKey(KeyCode.W)) {
                p_Velocity += new Vector3(0, 0, 1);
            }
            if (Input.GetKey(KeyCode.S)) {
                p_Velocity += new Vector3(0, 0, -1);
            }
            if (Input.GetKey(KeyCode.A)) {
                p_Velocity += new Vector3(-1, 0, 0);
            }
            if (Input.GetKey(KeyCode.D)) {
                p_Velocity += new Vector3(1, 0, 0);
            }
            return p_Velocity;
        }
    }
}