using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel : IComparable<Voxel> {
    public const float CLOUD_TRANSPARENCY_SCALE = 0.1F;
    
    public readonly double Latitude, Longitude, Altitude, Cloud;

    private readonly Color GrayScaleColor;
    private readonly Material GrayScaleColorMaterial, RandomColorMaterial;
    private readonly System.Random Random;
    private bool usingGrayScaleColor = false;

    private Vector3 size = new Vector3();
    public readonly Transform CubeClone;

    protected Renderer renderer { get; private set; }

    public Vector3 Size {
        private get => size;
        set {
            size = value;

            // Calculate bounds based on size.
            float upBoundLatitude = (float) Latitude + Size.x;
            if (upBoundLatitude > 90) upBoundLatitude = 180 - upBoundLatitude;
            float downBoundLatitude = (float) Latitude - Size.x;
            if (downBoundLatitude < -90) downBoundLatitude = -180 - downBoundLatitude;
            float leftBoundLongitude = (float) Longitude - Size.y;
            if (leftBoundLongitude <= -180) leftBoundLongitude += 360;
            float rightBoundLongitude = (float) Longitude + Size.y;
            if (rightBoundLongitude > 180) rightBoundLongitude -= 360;
            float lowBoundAltitude = (float) Altitude - Size.z;
            float highBoundAltitude = (float) Altitude + Size.z;

            // Calculate size by converting the earth coordinates of each neighbor (on all 6 sides) to cartesian.
            Vector3[] verticalBounds = new Vector3[] {
                Coordinate.SphericalToCartesian(
                    Coordinate.EarthToSpherical(upBoundLatitude, (float) Longitude, (float) Altitude)),
                Coordinate.SphericalToCartesian(
                    Coordinate.EarthToSpherical(downBoundLatitude, (float) Longitude, (float) Altitude))
            };
            Vector3[] horizontalBounds = new Vector3[] {
                Coordinate.SphericalToCartesian(
                    Coordinate.EarthToSpherical((float) Latitude, leftBoundLongitude, (float) Altitude)),
                Coordinate.SphericalToCartesian(
                    Coordinate.EarthToSpherical((float) Latitude, rightBoundLongitude, (float) Altitude))
            };
            Vector3[] heightBounds = new Vector3[] {
                Coordinate.SphericalToCartesian(
                    Coordinate.EarthToSpherical((float) Latitude, (float) Longitude, lowBoundAltitude)),
                Coordinate.SphericalToCartesian(
                    Coordinate.EarthToSpherical((float) Latitude, (float) Longitude, highBoundAltitude))
            };

            float verticalNeighborDistance = Vector3.Distance(CubeClone.localPosition, verticalBounds[0])
                + Vector3.Distance(CubeClone.localPosition, verticalBounds[1]);
            float horizontalNeighborDistance = Vector3.Distance(CubeClone.localPosition, horizontalBounds[0])
                + Vector3.Distance(CubeClone.localPosition, horizontalBounds[1]);
            float heightNeighborDistance = Vector3.Distance(CubeClone.localPosition, heightBounds[0])
                + Vector3.Distance(CubeClone.localPosition, heightBounds[1]);

            // Set size. The x-coordinate is for vertical size and the y-coordinate is for horizontal size. That seems
            // backwards but it's the way it is.
            CubeClone.localScale = new Vector3(
                horizontalNeighborDistance / 2,
                verticalNeighborDistance / 2,
                heightNeighborDistance / 2);
            }
    }

    public Voxel(double latitude, double longitude, double altitude, double cloud, Transform cube) {

        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
        Cloud = cloud;

        CubeClone = UnityEngine.Object.Instantiate(cube.gameObject, cube.parent).GetComponent<Transform>();
        CubeClone.gameObject.name = "[" + latitude + ", " + longitude + ", " + altitude + " | " + cloud + "]";

        // Adjust transparency according to cloud value.
        renderer = CubeClone.gameObject.GetComponent<Renderer>();
        GrayScaleColor = new Color(1, 1, 1, (float) cloud * CLOUD_TRANSPARENCY_SCALE * CLOUD_TRANSPARENCY_SCALE);
        GrayScaleColorMaterial = renderer.material;
        //GrayScaleColorMaterial.SetColor("_Color", GrayScaleColor);

        // Random color for debugging.
        RandomColorMaterial = new Material(Shader.Find("Standard"));
        Random = new System.Random();

        ToggleRandomColor();

        // Set position.
        CubeClone.localPosition = Coordinate.SphericalToCartesian(
            Coordinate.EarthToSpherical((float) Latitude, (float) Longitude, (float) Altitude));
        
        // Set rotation.
        CubeClone.eulerAngles = new Vector3((float) Latitude, (float) -Longitude, 0);
    }

    public Voxel(double[] data, Transform cube) : this(
        data[0], data[1], data[2], data[3], cube) {}
    
    public Voxel(int latitude, int longitude, int altitude, int cloud, Transform cube) : this(
        (double) latitude, (double) longitude, (double) altitude, (double) cloud, cube) {}

    public void ShowHideThreshold(float threshold) {
        renderer.enabled = threshold <= Cloud;
    }

    public float CloudTransparencyScale {
        private get => renderer.material.GetColor("_Color").a;
        set {
            // renderer.material.SetColor(
            //     "_Color", new Color(1, 1, 1, (float) Cloud * value * value));
        }
    }

    public void ToggleRandomColor() {
        if (usingGrayScaleColor) {
            RandomColorMaterial.SetColor("_Color", new Color(
                (float) Random.NextDouble(),
                (float) Random.NextDouble(),
                (float) Random.NextDouble()));
            renderer.material = RandomColorMaterial;
            usingGrayScaleColor = false;
        } else {
            renderer.material = GrayScaleColorMaterial;
            usingGrayScaleColor = true;
        }
    }

    public int CompareTo(Voxel other) {
        // if (Latitude != other.Latitude) return Latitude.CompareTo(other.Latitude);
        // if (Longitude != other.Longitude) return Longitude.CompareTo(other.Longitude);
        // return Altitude.CompareTo(other.Altitude);
        if (Altitude != other.Altitude) return Altitude.CompareTo(other.Altitude);
        if (Longitude != other.Longitude) return Longitude.CompareTo(other.Longitude);
        return Latitude.CompareTo(other.Latitude);
    }

    public override string ToString() {
        return "[ latitude: " + Latitude + ", Longitude: "
            + Longitude + ", Altitude: "
            + Altitude + ", Cloud: " + Cloud + " ]";
    }
}
