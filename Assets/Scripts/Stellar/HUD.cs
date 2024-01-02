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
        // 1 degree latitude at 111 km
        Vector3 current_loc = Camera.main.transform.position;
        float lat = 42.5f + (current_loc.z - 180.6f) * (1.0f / 111000.0f);
        float lng_weight = (lat - 40f) / 5f;
        float lng_scale = 85000.0f * (1.0f - lng_weight) + (79000.0f * lng_weight);
        float lng = 75.0f - (current_loc.x + 406.3f) * (1.0f / lng_scale);
        float alt = (current_loc.y + 100.0f);

        string latStr = String.Format("{0:.000}", lat) + "°N";
        string lngStr = String.Format("{0:.000}", lng) + "°W";
        string altStr = String.Format("{0:0}", alt) + " meters";

        HUD_text.text = "(" + latStr + ", " + lngStr + ") " + altStr;
    }
}
