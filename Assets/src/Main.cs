// using HDF.Pinvoke;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Main : MonoBehaviour {
    // private const string CLOUT_DATA_FILE = "cloud.csv";
    private const string CLOUT_DATA_FILE = "cloudsat_cloud_2019_180_UTC-18.24.26-18.25.50.csv";
    private const int ALTITUDE_LABEL_INDEX = 2;

    [SerializeField]
    private Transform cube;

    private WidgetHandler widgetHandler;

    private Voxel[] voxels;
    private double[][] existingCoordinates;

    private double[][] LoadCsvData(string filename) {
        string[] currentDirectoryPathArray = System.IO.Directory.GetCurrentDirectory().Split('\\');
        string dataPath = string.Join('\\',
            currentDirectoryPathArray.Take(currentDirectoryPathArray.Length - (Application.isEditor ? 3 : 0)))
            + "\\data\\" + filename;
        Debug.Log("Loading data from: " + dataPath);
        StreamReader streamReader = new StreamReader(dataPath);
        List<double[]> data = new List<double[]>();

        // First line is just labels.
        bool altitudeUnitKilometers = IsAltitudeUnitKilometers(
            streamReader.ReadLine().Split(',')[ALTITUDE_LABEL_INDEX]);

        while (!streamReader.EndOfStream) {
            string[] line = streamReader.ReadLine().Split(',');
            double[] lineDoubles = new double[line.Length];
            for (int i = 0; i < line.Length; i++) {
                lineDoubles[i] = double.Parse(line[i]);
                // If using kilometers as the altitude unit, multiply by 1000 to convert it to meters.
                if (altitudeUnitKilometers && i == ALTITUDE_LABEL_INDEX) lineDoubles[i] *= 1000;
            }
            data.Add(lineDoubles);
        }
        return data.ToArray();
    }

    private void LoadHdfData(string filename) {
        string[] currentDirectoryPathArray = System.IO.Directory.GetCurrentDirectory().Split('\\');
        string dataPath = string.Join('\\', currentDirectoryPathArray.Take(currentDirectoryPathArray.Length - 3))
                + "\\data\\cloudsat\\" + filename;
        Debug.Log("Loading data from: " + dataPath);
    }

    private Voxel[] SetupVoxels(double[][] data) {
        // Instantiate the voxels.
        List<Voxel> voxelList = new List<Voxel>();
        for (int i = 0; i < data.Length; i++) {
            voxelList.Add(new Voxel(data[i], cube));
        }
        Destroy(cube.gameObject);

        // Order voxels by latitude, then longitude, then altitude.
        voxelList.Sort();

        // Make the order of voxel gameObjects match the order we set here.
        for (int i = 0; i < data.Length; i++) {
            voxelList[i].CubeClone.SetAsLastSibling();
        }

        // Calculate voxel size based on how far apart the voxels are spaced from their neighbors.
        double minimumLatitudeSpacing = double.MaxValue;
        double minimumLongitudeSpacing = double.MaxValue;
        for (int i = 0; i < data.Length - 1; i++) {
            double latitudeSpacing = Math.Abs(data[i][0] - data[i + 1][0]);
            if (latitudeSpacing > 0 && latitudeSpacing < minimumLatitudeSpacing)
                minimumLatitudeSpacing = latitudeSpacing;
            double longitudeSpacing = Math.Abs(data[i][1] - data[i + 1][1]);
            if (longitudeSpacing > 0 && longitudeSpacing < minimumLongitudeSpacing)
                minimumLongitudeSpacing = longitudeSpacing;
        }

        for (int i = 0; i < voxelList.Count; i++) {
            // Altitude distance apart varies among voxels, so calculate that seperately.
            float altitudeSpacing = 200; // Magic number for now.

            // Set size.
            voxelList[i].Size = new Vector3(
                (float) minimumLatitudeSpacing,
                (float) minimumLongitudeSpacing,
                altitudeSpacing);
        }

        return voxelList.ToArray();
    }

    private bool IsAltitudeUnitKilometers(string altitudeLabel) {
        if (altitudeLabel.ToLower().Contains("meter")) {
            return (altitudeLabel.ToLower().Contains("kilo"));
        }
        return true;
    }

    // Start is called before the first frame update.
    private void Start() {
        voxels = SetupVoxels(LoadCsvData(CLOUT_DATA_FILE));
        GetComponent<WidgetHandler>().Voxels = voxels;

        // LoadHdfData("2B-GEOPROF.P1_R05\\2015\\180\\2015180002346_48770_CS_2B-GEOPROF_GRANULE_P1_R05_E06_F00.hdf");
    }

    // Update is called once per frame.
    private void Update() {
        
    }
}
