using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    [DllImport("user32.dll")]
    public static extern short GetKeyState(int keyCode);

    [SerializeField]
    private float travelSpeed = 10, hasteFactor = 5, extraHasteFactor = 3, dampFactor = 5, lookSensitivity = 100;

    private enum USER_KEY_CONTROL {
        LEFT, RIGHT, FORWARD, BACKWARD, ASCEND, DESCEND, HASTEN, DAMPEN
    }

    private enum USER_MOUSE_CONTROL {
        UP, DOWN, LEFT, RIGHT
    }

    private bool[] keyCommands = new bool[Enum.GetValues(typeof(USER_KEY_CONTROL)).Length];

    private Vector2 mouseMoveAxis;

    // Start is called before the first frame update.
    void Start() {
        
    }

    // Update is called once per frame.
    void Update() {
        keyCommands[(int) USER_KEY_CONTROL.LEFT] = Input.GetKey(KeyCode.A);
        keyCommands[(int) USER_KEY_CONTROL.RIGHT] = Input.GetKey(KeyCode.D);
        keyCommands[(int) USER_KEY_CONTROL.FORWARD] = Input.GetKey(KeyCode.W);
        keyCommands[(int) USER_KEY_CONTROL.BACKWARD] = Input.GetKey(KeyCode.S);
        keyCommands[(int) USER_KEY_CONTROL.ASCEND] = Input.GetKey(KeyCode.E);
        keyCommands[(int) USER_KEY_CONTROL.DESCEND] = Input.GetKey(KeyCode.Q);
        keyCommands[(int) USER_KEY_CONTROL.HASTEN] =
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        keyCommands[(int) USER_KEY_CONTROL.DAMPEN] =
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        
        mouseMoveAxis = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        Debug.Log(mouseMoveAxis);
    }

    void FixedUpdate() {
        // Get update rates per frame.
        float travelSpeedPerFrame = travelSpeed * Time.fixedDeltaTime;

        if (keyCommands[(int) USER_KEY_CONTROL.HASTEN]) {
            travelSpeedPerFrame *= hasteFactor;
            if ((((ushort) GetKeyState(0x14)) & 0xffff) != 0)
                travelSpeedPerFrame *= extraHasteFactor;
        }
            
        if (keyCommands[(int) USER_KEY_CONTROL.DAMPEN])
            travelSpeedPerFrame /= dampFactor;

        if (keyCommands[(int) USER_KEY_CONTROL.LEFT])
            transform.localPosition -= transform.right * travelSpeedPerFrame;
        if (keyCommands[(int) USER_KEY_CONTROL.RIGHT])
            transform.localPosition += transform.right * travelSpeedPerFrame;
        if (keyCommands[(int) USER_KEY_CONTROL.FORWARD])
            transform.localPosition += transform.forward * travelSpeedPerFrame;
        if (keyCommands[(int) USER_KEY_CONTROL.BACKWARD])
            transform.localPosition -= transform.forward * travelSpeedPerFrame;
        if (keyCommands[(int) USER_KEY_CONTROL.ASCEND])
            transform.localPosition += transform.up * travelSpeedPerFrame;
        if (keyCommands[(int) USER_KEY_CONTROL.DESCEND])
            transform.localPosition -= transform.up * travelSpeedPerFrame;
        
        transform.RotateAround(
            transform.position, transform.up, mouseMoveAxis.x * lookSensitivity * Time.deltaTime);
        transform.RotateAround(
            transform.position, transform.right, -mouseMoveAxis.y * lookSensitivity * Time.deltaTime);
    }
}
