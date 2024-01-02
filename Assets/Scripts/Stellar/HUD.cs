using UnityEngine;
using System;
using TMPro;

public class HUD : MonoBehaviour 
{

    public TMP_Text HUD_text;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 1 degree longitude at 40 degrees North latitude is 85 km
        // 1 degree longitude at 45 degrees North latitude is 79 km
        float CENTER_LONGITUDE = 42.5f;
        float CENTER_LATITUDE = -75.0f;
        float METERS_PER_DEG_LONG = 111111f;
        float z0 = 180.6f;
        float x0 = -406.3f;
        float y0 = 0f;

        Vector3 current_loc = Camera.main.transform.position;
        float lat = CENTER_LONGITUDE + (current_loc.z - z0) * (1.0f / METERS_PER_DEG_LONG);
        float meters_per_degree_lat = METERS_PER_DEG_LONG * Mathf.Cos(lat * Mathf.Deg2Rad);
        float lng = -CENTER_LATITUDE - (current_loc.x - x0) * (1.0f / meters_per_degree_lat);
        float alt = (current_loc.y - y0);

        string latStr = String.Format("{0:.000}", lat) + "°N";
        string lngStr = String.Format("{0:.000}", lng) + "°W";
        string altStr = String.Format("{0:0}", alt) + " meters";

        HUD_text.text = "(" + latStr + ", " + lngStr + ") " + altStr;
    }
}
