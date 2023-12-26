using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WidgetHandler : MonoBehaviour {
    private const string EMPTY_TARGET_COORDINATES_TEXT = "→\n→\n→\n→";
    private const int DEAD_ZONE_DIVISOR = 100;

    [SerializeField]
    private TextMeshProUGUI transparencyLabel, thresholdLabel,
        coordinatesLookingText, coordinatesMarkerText, coordinatesTargetText;

    [SerializeField]
    private Transform inputFieldParent;
    [SerializeField]
    private TMP_InputField latitudeInputField, longitudeInputField, altitudeInputField, orientationInputField;

    [SerializeField]
    private int incrementMultiplier;

    private float[] defaultSliderValues = new float[] { 0, Voxel.CLOUD_TRANSPARENCY_SCALE, 0 };

    private Voxel[] voxels;
    private double[] voxelCoordinatesLatitude, voxelCoordinatesLongitude, voxelCoordinatesAltitude;

    private MouseHoverBehavior targetTextHoverDetector;

    private Vector4 lookingCoordinates = new Vector4(0, 0, 0, 0);

    private bool activeInputFields = false, inputFieldsChanged = false;

    public Voxel[] Voxels {
        private get => voxels;
        set {
            voxels = value;
            SortedSet<double> coordinateSetLatitude = new SortedSet<double>(),
                coordinateSetLongitude = new SortedSet<double>(),
                coordinateSetAltitude = new SortedSet<double>();
            foreach (Voxel voxel in voxels) {
                coordinateSetLatitude.Add(voxel.Latitude);
                coordinateSetLongitude.Add(voxel.Longitude);
                coordinateSetAltitude.Add(voxel.Altitude);
            }
            voxelCoordinatesLatitude = new double[coordinateSetLatitude.Count];
            voxelCoordinatesLongitude = new double[coordinateSetLongitude.Count];
            voxelCoordinatesAltitude = new double[coordinateSetAltitude.Count];
            coordinateSetLatitude.CopyTo(voxelCoordinatesLatitude);
            coordinateSetLongitude.CopyTo(voxelCoordinatesLongitude);
            coordinateSetAltitude.CopyTo(voxelCoordinatesAltitude);
        }
    }

    // Called by WalkScript every frame. Uses the coordinates returned by this method to move the camera to them.
    public Vector4? InputTargetCoordinates {
        get {
            if (inputFieldsChanged) {
                inputFieldsChanged = false;
                return ApplyFieldInputs();
            }
            return null;
        }
    }

    // Called by UI slider component whenever it changes. Updates whether voxels are visible if their value is too low.
    public void OnThresholdSliderChange(float value) {
        float roundedValue = Mathf.Round(value * 10000) / 10000F;
        thresholdLabel.text = "Threshold: >" + roundedValue;
        if (voxels != null) {
            foreach (Voxel voxel in voxels) {
                voxel.ShowHideThreshold(roundedValue);
            }
        }
    }

    // Called by UI slider component whenever it changes. Updates voxels' transparency scale.
    public void OnTransparencySliderChange(float value) {
        float roundedValue = Mathf.Round(value * 100) / 100F;
        transparencyLabel.text = "Transparency: " + (roundedValue * 100) + "%";
        if (voxels != null) {
            foreach (Voxel voxel in voxels) {
                voxel.CloudTransparencyScale = roundedValue;
            }
        }
    }

    // Called by UI button when it's pressed. Resets the sliders.
    public void OnResetSliders() {
        GameObject.Find("FOV Slider").GetComponent<Slider>().value = defaultSliderValues[0];
        GameObject.Find("Transparency Slider").GetComponent<Slider>().value = defaultSliderValues[1];
        GameObject.Find("Threshold Slider").GetComponent<Slider>().value = defaultSliderValues[2];
    }

    // Called by UI arrow buttons next to text fields for coordinates when they're pressed.
    public void OnTargetInputButtonPress(int buttonId) {
        switch (buttonId) {
            case 0:
                // Latitude increment
                IncrementInputField(true, Coordinate.ID.LATITUDE);
                break;
            case 1:
                // Latitude decrement
                IncrementInputField(false, Coordinate.ID.LATITUDE);
                break;
            case 2:
                // Longitude increment
                IncrementInputField(true, Coordinate.ID.LONGITUDE);
                break;
            case 3:
                // Longitude decrement
                IncrementInputField(false, Coordinate.ID.LONGITUDE);
                break;
            case 4:
                // Altitude increment
                IncrementInputField(true, Coordinate.ID.ALTITUDE);
                break;
            case 5:
                // Altitude decrement
                IncrementInputField(false, Coordinate.ID.ALTITUDE);
                break;
            case 6:
                // Orientation increment
                IncrementOrientationInputField(true);
                break;
            case 7:
                // Orientation decrement
                IncrementOrientationInputField(false);
                break;
            default:
                Debug.LogError("Error: Invalid button ID: " + buttonId);
                break;
        }
    }

    public void IncrementOrientationInputField(bool clockwise) {

    }

    public void IncrementInputField(bool increment, Coordinate.ID coordinateId) {
        double[] voxelCoordinates;
        if (coordinateId == Coordinate.ID.LATITUDE) voxelCoordinates = voxelCoordinatesLatitude;
        else if (coordinateId == Coordinate.ID.LONGITUDE) voxelCoordinates = voxelCoordinatesLongitude;
        else voxelCoordinates = voxelCoordinatesAltitude;

        TMP_InputField inputField = null;
        float? lookingCoordinate = null;
        if (coordinateId == Coordinate.ID.LATITUDE) {
            inputField = latitudeInputField;
            lookingCoordinate = lookingCoordinates.x;
        } else if (coordinateId == Coordinate.ID.LONGITUDE) {
            inputField = longitudeInputField;
            lookingCoordinate = lookingCoordinates.y;
        } else if (coordinateId == Coordinate.ID.ALTITUDE) {
            inputField = altitudeInputField;
            lookingCoordinate = lookingCoordinates.z;
        } else Debug.LogError("Error: Invalid coordinate ID: " + Enum.GetName(typeof(Coordinate.ID), coordinateId));

        int amount = (increment ? 1 : -1)
            * ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? incrementMultiplier : 1);

        double inputValueParsed;
        int? newIndex = null;
        if (!double.TryParse(inputField.text, out inputValueParsed)) {
            // Use the looking coordinate if the text in the field can't be parsed.
            if (lookingCoordinate.HasValue) inputValueParsed = lookingCoordinate.Value;
            else {
                Debug.LogError("Error: Looking coordinate missing.");
                return;
            }
        }

        newIndex = CalculateInputFieldNewIndex(voxelCoordinates, amount, inputValueParsed);
        if (newIndex.HasValue) {
            inputField.text = voxelCoordinates[newIndex.Value].ToString();
            inputFieldsChanged = true;
        } else Debug.LogError("Error: Could not determine voxel index.");
    }

    // Called by WalkScript every frame. WalkScript tells whether the user left-clicked the mouse or pressed the ENTER
    // key this frame.
    public bool HasFocus(bool clicked, bool pressedEnter) {
        if (activeInputFields) {
            if (pressedEnter) {
                activeInputFields = false;
                inputFieldParent.gameObject.SetActive(false);
                inputFieldsChanged = true;
                coordinatesTargetText.gameObject.SetActive(true);
            } else if (!inputFieldsChanged) return true;
        }
        if (EventSystem.current.IsPointerOverGameObject()) {
            if (targetTextHoverDetector.Hovered && clicked) {
                activeInputFields = true;
                inputFieldParent.gameObject.SetActive(true);
                coordinatesTargetText.text = EMPTY_TARGET_COORDINATES_TEXT;
                return true;
            }
        }
        return false;
    }

    // Called by WalkScript every frame. WalkScript tells what the Earth coordinates are of the ray pointing from the
    // center of the camera to the ground.
    public void SetLookingCoordinates(float longitude, float latitude, float altitude, float orientation) {
        Vector4 earthCoordinatesRounded = new Vector4(
            Mathf.Round(longitude * 100) / 100,
            Mathf.Round(latitude * 100) / 100,
            Mathf.Round(altitude / 10) / 100, // Divide by an extra 1000 to make it kilometers instead of meters.
            Mathf.Round(orientation * 100) / 100);
        coordinatesLookingText.text =
            "Longitude:\t" + (longitude >= 0 ? " " : "") + earthCoordinatesRounded.x
            + "\nLatitude:\t\t" + (latitude >= 0 ? " " : "") + earthCoordinatesRounded.y
            + "\nAltitude:\t\t" + (altitude >= 0 ? " " : "") + earthCoordinatesRounded.z
            + "\nOrientation:\t" + (orientation >= 0 ? " " : "") + earthCoordinatesRounded.w;
        lookingCoordinates = new Vector4(longitude, latitude, altitude, orientation);
    }

    // Called by WalkScript every frame while in orbital view. WalkScript tells what the Earth coordinates of the
    // green marker are (the marker that shows where over the Earth the mouse is hovering). 
    public void SetMarkerCoordinates(float longitude, float latitude) {
        Vector2 earthCoordinatesRounded = new Vector2(
            Mathf.Round(longitude * 100) / 100,
            Mathf.Round(latitude * 100) / 100);
        coordinatesMarkerText.text =
            "Longitude:\t" + (longitude >= 0 ? " " : "") + earthCoordinatesRounded.x
            + "\nLatitude:\t\t" + (latitude >= 0 ? " " : "") + earthCoordinatesRounded.y;
    }

    // Called by WalkScript when the user left-clicks while hovering the mouse over the Earth to place a red marker.
    // WalkScript tells what the Earth coordinates of the red marker are.
    // Called by WidgetHandler from ApplyFieldInputs as a result of the user pressing the ENTER key after filling in
    // the coordinate text fields.
    public void SetTargetCoordinatesText(float longitude, float latitude, float altitude, float orientation) {
        Vector4 earthCoordinatesRounded = new Vector4(
            Mathf.Round(longitude * 100) / 100,
            Mathf.Round(latitude * 100) / 100,
            Mathf.Round(altitude / 10) / 100, // Divide by an extra 1000 to make it kilometers instead of meters.
            Mathf.Round(orientation * 100) / 100);
        coordinatesTargetText.text =
            "→ " + (longitude >= 0 ? " " : "") + earthCoordinatesRounded.x
            + "\n→ " + (latitude >= 0 ? " " : "") + earthCoordinatesRounded.y
            + "\n→ " + (altitude >= 0 ? " " : "") + earthCoordinatesRounded.z
            + "\n→ " + (orientation >= 0 ? " " : "") + earthCoordinatesRounded.w;
        coordinatesTargetText.gameObject.SetActive(true);
    }

    // Called by WalkScript when the user pressed the C key. Toggles the voxels between being white (which looks grey)
    // and being a random color. The colors are randomized every time they're toggled to be random.
    public void ToggleVoxelsColor() {
        foreach (Voxel voxel in voxels) {
            voxel.ToggleRandomColor();
        }
    }

    // Called by WidgetHandler by the InputTargetCoordinates setter, which calls this method whenever
    // inputFieldsChanged is set to true.
    private Vector4 ApplyFieldInputs() {
        float latitudeInputValue = lookingCoordinates.x;
        float latitudeInputValueParsed;
        if (float.TryParse(latitudeInputField.text, out latitudeInputValueParsed)) {
            latitudeInputValue = latitudeInputValueParsed;
        }

        float longitudeInputValue = lookingCoordinates.y;
        float longitudeInputValueParsed;
        if (float.TryParse(longitudeInputField.text, out longitudeInputValueParsed)) {
            longitudeInputValue = longitudeInputValueParsed;
        }

        float altitudeInputValue = lookingCoordinates.z;
        float altitudeInputValueParsed;
        if (float.TryParse(altitudeInputField.text, out altitudeInputValueParsed)) {
            altitudeInputValue = altitudeInputValueParsed;
        }

        float orientationInputValue = lookingCoordinates.w;
        float orientationInputValueParsed;
        if (float.TryParse(orientationInputField.text, out orientationInputValueParsed)) {
            orientationInputValue = orientationInputValueParsed;
        }

        SetTargetCoordinatesText(latitudeInputValue, longitudeInputValue, altitudeInputValue, orientationInputValue);
        return new Vector4(latitudeInputValue, longitudeInputValue, altitudeInputValue, orientationInputValue);
    }

    // Assumes that voxelCoordinates is sorted in ascending order and equally spaced apart.
    private int? CalculateInputFieldNewIndex(double[] voxelCoordinates, int amount, double inputValue) {
        double spacing = voxelCoordinates[1] - voxelCoordinates[0],
            deadZone = spacing / DEAD_ZONE_DIVISOR,
            newInputValue = inputValue += (amount * spacing);
        int? newIndex = null;
        if (newInputValue < voxelCoordinates[0]) newIndex = 0;
        else if (newInputValue > voxelCoordinates[voxelCoordinates.Length - 1])
            newIndex = voxelCoordinates.Length;
        else {
            int indexToCompare = voxelCoordinates.Length / 2,
                indexToCompareMin = 0,
                indexToCompareMax = voxelCoordinates.Length - 1;
            double differenceFromInputToCoordinate = Math.Abs(voxelCoordinates[indexToCompare] - newInputValue);
            while (differenceFromInputToCoordinate >= deadZone) {
                if (newInputValue > voxelCoordinates[indexToCompare]) indexToCompareMin = indexToCompare;
                else indexToCompareMax = indexToCompare;

                indexToCompare = ((indexToCompareMax - indexToCompareMin) / 2) + indexToCompareMin;
                if (indexToCompare == indexToCompareMax || indexToCompare == indexToCompareMin) {
                    double differenceWithMax = Math.Abs(voxelCoordinates[indexToCompareMax] - newInputValue),
                        differenceWithMin = Math.Abs(voxelCoordinates[indexToCompareMin] - newInputValue);
                    if (differenceWithMax < differenceWithMin) return indexToCompareMax;
                    return indexToCompareMin;
                }

                differenceFromInputToCoordinate = Math.Abs(voxelCoordinates[indexToCompare] - newInputValue);
            }
            newIndex = indexToCompare;
        }

        if (newIndex.HasValue) {
            if (newIndex.Value < 0) return 0;
            if (newIndex.Value >= voxelCoordinates.Length) return voxelCoordinates.Length - 1;
            return newIndex.Value;
        }
        return null;
    }

    // Start is called before the first frame update.
    private void Start() {
        defaultSliderValues[0] = GameObject.Find("FOV Slider").GetComponent<Slider>().value;
        OnResetSliders();

        targetTextHoverDetector = coordinatesTargetText.GetComponent<MouseHoverBehavior>();

        if (voxelCoordinatesLatitude != null && voxelCoordinatesLongitude != null) {
            // Get middle latitude and longitude and set the text fields to make the inputTargetCoordinates have those
            // values. This is so that the user starts within the region of interest.
            latitudeInputField.text = (voxelCoordinatesLatitude[voxelCoordinatesLatitude.Length / 2]).ToString();
            longitudeInputField.text = (voxelCoordinatesLongitude[voxelCoordinatesLongitude.Length / 2]).ToString();
            inputFieldsChanged = true;
        } else {
            Debug.LogError("Voxel coordinate arrays not instantiated.");
        }
    }

    // Update is called once per frame.
    private void Update() {
        
    }
}
