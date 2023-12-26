using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WalkScript : MonoBehaviour {
    [SerializeField]
    private WidgetHandler widgetHandler;

    [SerializeField]
    private Transform earth, sunlight, cameraChild, cameraDummy, mouseMarker, targetMarker;

    [SerializeField]
    private UnityEngine.UI.Toggle lockSunlightToggle;

    [SerializeField]
    private float mouseSensitivity, scrollSensitivity, keySensitivity, sensitivityMultiplier,
        lerpSpeed, minimumAltitude, maximumAltitude;

    private Camera cameraFromChild;

    private Vector2 keyInputAxis, keyInputAxisSmooth, keyAxisSmoothVelocity;
    private float keyInputRotate, keyInputRotateSmooth, keyRotateSmoothVelocity,
        scrollInput, scrollInputFixed, scrollInputSmooth, scrollSmoothVelocity;
    // For when not in orbital view, pressing a directional key will only increment the camera's target by 1 until the
    // user depresses the key and presses again.
    private bool[] keyInputAxisDown = { false, false };

    private Vector2 mouseMoveAxis;

    private float modifySensitivity, lerpDistance = 0;
    private Vector3 mouseMarkerOriginalSize, targetMarkerOriginalSize;

    private Vector4? lerpFromPosition = null, lerpToPosition = null;
    private float? lerpFromZoom = null, lerpToZoom = null, lerpFromOrient = null, lerpToOrient = null;

    private bool lockSunlight = false, orbitalView = true;

    private float originalOrbitalDistance = 0;

    private bool Lerping { get => lerpFromPosition.HasValue && lerpToPosition.HasValue; }

    private float MinimumAltitude { get => Coordinate.EARTH_RADIUS_UNITY + minimumAltitude; }
    private float MaximumAltitude { get => Coordinate.EARTH_RADIUS_UNITY + maximumAltitude; }

    public void ToggleLockSunlight() { lockSunlight = lockSunlightToggle.isOn; }

    // Start is called before the first frame update.
    private void Start() {
        cameraFromChild = cameraChild.gameObject.GetComponent<Camera>();
        mouseMarkerOriginalSize = mouseMarker.localScale;
        targetMarkerOriginalSize = targetMarker.localScale;
        originalOrbitalDistance = cameraChild.localPosition.z;
    }

    // Update is called once per frame.
    private void Update() {
        bool hasFocus = widgetHandler.HasFocus(
            Input.GetMouseButtonDown(0),
            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape));

        // Use key and mouse input to pan and toggle, but only if the widgets don't have focus.
        if (!hasFocus) {
            // Exit the application if user presses the ESC key.
            if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();

            // Get SPACE key input.
            if (Input.GetKeyDown(KeyCode.Space)) {
                orbitalView = !orbitalView;
                if (!orbitalView) {
                    mouseMarker.gameObject.SetActive(false);
                    targetMarker.gameObject.SetActive(false);
                    Cursor.visible = false;

                    // Rotate camera to look away from the earth.
                    cameraChild.localEulerAngles = new Vector3(0, 180, 0);
                    // Move camera to default position (the ground) by pushing the altitude increment button.
                    widgetHandler.IncrementInputField(true, Coordinate.ID.ALTITUDE);
                } else {
                    // Set camera to look towards the earth.
                    cameraChild.localEulerAngles = new Vector3(0, 0, 0);
                    // Move camera to ground level.
                    cameraChild.localPosition = new Vector3(0, 0, originalOrbitalDistance);
                }
            }

            // Get SHIFT key input.
            modifySensitivity = 1;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                modifySensitivity = sensitivityMultiplier;
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftControl))
                modifySensitivity = 1 / sensitivityMultiplier / 3;

            // Get WASD key input.
            keyInputAxis = new Vector2(
                Input.GetAxisRaw("Horizontal") * keySensitivity,
                Input.GetAxisRaw("Vertical") * keySensitivity).normalized;

            // Get QE key input.
            if (Input.GetKey("e")) keyInputRotate = 1;
            else if (Input.GetKey("q")) keyInputRotate = -1;
            else keyInputRotate = 0;

            // Get C key input.
            if (Input.GetKeyDown("c")) widgetHandler.ToggleVoxelsColor();

            // Get mouse scroll input.
            scrollInput = Input.mouseScrollDelta.y;
            if (scrollInput != 0) scrollInputFixed = scrollInput;
        }

        if (!orbitalView) {
            // Get mouse movement input.
            if (!hasFocus) mouseMoveAxis = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            else mouseMoveAxis = new Vector2(0, 0);
        }

        // Make the input vectors smooth.
        keyInputAxisSmooth = Vector2.SmoothDamp(keyInputAxisSmooth, keyInputAxis, ref keyAxisSmoothVelocity, 0.1F);
        keyInputRotateSmooth = Mathf.SmoothDamp(keyInputRotateSmooth, keyInputRotate, ref keyRotateSmoothVelocity, 0.1F);
        scrollInputSmooth = Mathf.SmoothDamp(scrollInputSmooth, scrollInput, ref scrollSmoothVelocity, 0.1F);

        // Adjust the size of the markers based on current altitude.
        mouseMarker.localScale =
            mouseMarkerOriginalSize * Mathf.Sqrt(-(cameraChild.localPosition.z + Coordinate.EARTH_RADIUS_UNITY));
        targetMarker.localScale =
            targetMarkerOriginalSize * Mathf.Sqrt(-(cameraChild.localPosition.z + Coordinate.EARTH_RADIUS_UNITY));

        // Use a ray to calculate latitude and longitude.
        Ray centerRay = cameraFromChild.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
        if (Physics.Raycast(centerRay, out RaycastHit centerRaycastHit)) {
            Vector2 earthCoordinates =
                Coordinate.SphericalToEarth(Coordinate.CartesianToSpherical(centerRaycastHit.point));
            // For displaying as text.
            widgetHandler.SetLookingCoordinates(
                earthCoordinates.x,
                earthCoordinates.y,
                CalculateAltitude(),
                CalculateOrientation());
        }
        
        Ray mouseRay = cameraFromChild.ScreenPointToRay(Input.mousePosition);
        if (hasFocus) {
            // Don't use a marker if the mouse is hovering over a widget.
            mouseMarker.gameObject.SetActive(false);
            Cursor.visible = true;
        } else if (orbitalView && Physics.Raycast(mouseRay, out RaycastHit mouseRaycastHit)) {
            // Use a ray from the mouse to place the marker.
            mouseMarker.gameObject.SetActive(orbitalView);
            mouseMarker.localPosition = mouseRaycastHit.point;
            mouseMarker.localRotation = Quaternion.LookRotation(earth.localPosition - mouseRaycastHit.point);
            mouseMarker.RotateAround(mouseMarker.localPosition, mouseMarker.right, 90);
            Cursor.visible = false;

            // Calculate the marker's latitude and longitude.
            Vector2 earthCoordinates = 
                Coordinate.SphericalToEarth(Coordinate.CartesianToSpherical(mouseMarker.localPosition));
            // For displaying as text.
            widgetHandler.SetMarkerCoordinates(earthCoordinates.x, earthCoordinates.y);

            // Place a target marker if the user clicks the left mouse button.
            if (Input.GetMouseButtonDown(0)) {
                targetMarker.localPosition = mouseMarker.localPosition;
                targetMarker.localRotation = mouseMarker.localRotation;

                targetMarker.gameObject.SetActive(orbitalView);

                // Set things up to lerp.
                lerpDistance = 0;
                lerpFromPosition = cameraChild.localPosition;

                // Rotate the camera parent opposite of which way the camera child would rotate if it was located to
                // the correct spot and then pointed at the Earth.
                Vector3 targetMarkerDirection = (earth.localPosition - targetMarker.localPosition).normalized;
                cameraDummy.localPosition = -(targetMarkerDirection * -cameraChild.localPosition.z);
                cameraDummy.LookAt(earth);
                lerpToPosition = cameraDummy.eulerAngles;
                lerpFromPosition = transform.localEulerAngles;

                // Update the text to show the marker's latitude and longitude in green with the arrows.
                widgetHandler.SetTargetCoordinatesText(
                    earthCoordinates.x,
                    earthCoordinates.y,
                    CalculateAltitude(),
                    CalculateOrientation());
            }
        } else if (!orbitalView) {
            
        } else {
            mouseMarker.gameObject.SetActive(false);
            Cursor.visible = true;
        }

        // Place a target marker if there is user input for coordinates.
        Vector4? inputTargetCoordinates = widgetHandler.InputTargetCoordinates;
        if (inputTargetCoordinates.HasValue) ApplyTargetCoordinates(inputTargetCoordinates.Value);

        // This only does anything if lerpFromCoordinate and lerpToCoordinate are not null.
        LerpAnglesUpdate();
    }

    private void ApplyTargetCoordinates(Vector4 targetCoordinates) {
        // Position of the target marker should have altitude of zero.
        targetMarker.localPosition = Coordinate.SphericalToCartesian(Coordinate.EarthToSpherical(new Vector3(
            targetCoordinates.x,
            targetCoordinates.y,
            0F)));
        // Rotate the target marker the same way the mouse marker gets rotated.
        targetMarker.localRotation = Quaternion.LookRotation(earth.localPosition - targetMarker.localPosition);
        targetMarker.RotateAround(targetMarker.localPosition, targetMarker.right, 90);

        targetMarker.gameObject.SetActive(orbitalView);

        // Set things up to lerp.
        lerpDistance = 0;
        lerpFromPosition = cameraChild.localPosition;

        // Need to apply the correct z-position by calculating cartesian coordinates using the altitude.
        lerpToZoom = Coordinate.EarthToSpherical(targetCoordinates).z;
        lerpFromZoom = -cameraChild.localPosition.z;

        // No need to process z-rotation.
        lerpToOrient = targetCoordinates.w;
        lerpFromOrient = transform.localEulerAngles.z;

        // Rotate the camera parent opposite of which way the camera child would rotate if it was located to
        // the correct spot and then pointed at the Earth.
        Vector3 targetMarkerDirection = (earth.localPosition - targetMarker.localPosition).normalized;
        cameraDummy.localPosition = -(targetMarkerDirection * -cameraChild.localPosition.z);
        cameraDummy.LookAt(earth);
        lerpToPosition = cameraDummy.eulerAngles;
        lerpFromPosition = transform.localEulerAngles;

        // The text to show the marker's latitude and longitude in green was already updated in the call to
        // HasFocus.
    }

    private void LerpAnglesUpdate() {
        if (Lerping) {
            lerpDistance += lerpSpeed;
            if (lerpDistance >= 1.0F) lerpDistance = 1.0F;

            // Move camera closer to its destination a little at a time.
            transform.localEulerAngles = LerpAngles(lerpFromPosition.Value, lerpToPosition.Value, lerpDistance);
            // Zoom is not always lerped.
            if (lerpFromZoom.HasValue && lerpToZoom.HasValue) {
                cameraChild.localPosition = new Vector3(
                    0,
                    0,
                    -Mathf.LerpAngle(lerpFromZoom.Value, lerpToZoom.Value, lerpDistance));
            }
            // Orientation is not always lerped.
            if (lerpFromOrient.HasValue && lerpToOrient.HasValue) {
                transform.localEulerAngles = new Vector3(
                    transform.localEulerAngles.x,
                    transform.localEulerAngles.y,
                    Mathf.LerpAngle(lerpFromOrient.Value, lerpToOrient.Value, lerpDistance));
            }

            if (lerpDistance == 1.0F) {
                transform.localEulerAngles = lerpToPosition.Value;
                // Zoom is not always lerped.
                if (lerpToZoom.HasValue) cameraChild.localPosition = new Vector3(0, 0, -lerpToZoom.Value);
                // Orientation is not always lerped.
                if (lerpToOrient.HasValue) transform.localEulerAngles = new Vector3(
                    transform.localEulerAngles.x,
                    transform.localEulerAngles.y,
                    lerpToOrient.Value);
                lerpFromPosition = null;
                lerpToPosition = null;
                lerpFromZoom = null;
                lerpToZoom = null;
                lerpFromOrient = null;
                lerpToOrient = null;
                lerpDistance = 0;
            }
        }
    }

    private void FixedUpdate() {
        if (orbitalView) {
           // Move the camera about the Earth's center.
            transform.RotateAround(
                earth.localPosition,
                transform.up,
                -keyInputAxisSmooth.x * keySensitivity * modifySensitivity * Time.deltaTime);
            transform.RotateAround(
                earth.localPosition,
                transform.right,
                keyInputAxisSmooth.y * keySensitivity * modifySensitivity * Time.deltaTime); 

            // Zoom the camera.
            cameraChild.localPosition += new Vector3(
                0,
                0,
                scrollInputSmooth * scrollSensitivity * modifySensitivity * Time.deltaTime);
        } else {
            // Move the camera by incrementing at the voxels.
            if (!keyInputAxisDown[0]) {
                if (keyInputAxis.x > 0) widgetHandler.IncrementInputField(true, Coordinate.ID.LATITUDE);
                else if (keyInputAxis.x < 0) widgetHandler.IncrementInputField(false, Coordinate.ID.LATITUDE);
            }
            if (!keyInputAxisDown[1]) {
                if (keyInputAxis.y > 0) widgetHandler.IncrementInputField(true, Coordinate.ID.LONGITUDE);
                else if (keyInputAxis.y < 0) widgetHandler.IncrementInputField(false, Coordinate.ID.LONGITUDE);
            }

            // Update whether the key is pressed or depressed.
            keyInputAxisDown[0] = (keyInputAxis.x != 0);
            keyInputAxisDown[1] = (keyInputAxis.y != 0);

            // Zoom the camera by incrementing at the voxels.
            if (scrollInputFixed > 0) widgetHandler.IncrementInputField(true, Coordinate.ID.ALTITUDE);
            else if (scrollInputFixed < 0) widgetHandler.IncrementInputField(false, Coordinate.ID.ALTITUDE);
            // scrollInput might be zero, in which case do nothing.

            // cameraChild.localRotation *= Quaternion.Euler(mouseMoveAxis.y * mouseSensitivity, mouseMoveAxis.x, 0);
            cameraChild.RotateAround(cameraChild.position, cameraChild.up, mouseMoveAxis.x * mouseSensitivity * Time.deltaTime);
            cameraChild.RotateAround(cameraChild.position, cameraChild.right, -mouseMoveAxis.y * mouseSensitivity * Time.deltaTime);
        }

        // scrollInputFixed is not set to zero in Update, so we set it to zero here.
        scrollInputFixed = 0;

        // Rotate the camera.
        transform.RotateAround(
            earth.localPosition,
            transform.forward,
            -keyInputRotateSmooth * keySensitivity * modifySensitivity * Time.deltaTime);
        
        // Don't zoom in through the ground or out too high.
        if (-cameraChild.localPosition.z < MinimumAltitude)
            cameraChild.localPosition = new Vector3(0, 0, -MinimumAltitude);
        else if (-cameraChild.localPosition.z > MaximumAltitude)
            cameraChild.localPosition = new Vector3(0, 0, -MaximumAltitude);

        // Have the sunlight follow the camera unless it's set to be locked.
        if (!lockSunlight) sunlight.LookAt(earth.localPosition - cameraChild.position);
    }

    private Vector3 LerpAngles(Vector3 a, Vector3 b, float t) {
        return new Vector3(
            Mathf.LerpAngle(a.x, b.x, t),
            Mathf.LerpAngle(a.y, b.y, t),
            Mathf.LerpAngle(a.z, b.z, t));
    }

    // Calculates the altitude as an Earth coordinate (in meters).
    private float CalculateAltitude() {
        return ((earth.localPosition - cameraChild.position).magnitude - Coordinate.EARTH_RADIUS_UNITY)
            * (float) Coordinate.REAL_RATIO;
    }

    private float CalculateOrientation() {
        // return Mathf.Asin(Vector3.Cross(transform.up, earth.up).magnitude);
        // return Mathf.Acos(Vector3.Dot(transform.up, earth.up));
        return transform.localEulerAngles.z;
    }
}
