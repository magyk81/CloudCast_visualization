using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Coordinate {
    // Earth diameter is 40.39 in Unity meters.
    public const float EARTH_RADIUS_UNITY = 20.195F;
    public const float EARTH_RADIUS_REAL = 6378.137F * 1000;
    public const float REAL_RATIO = EARTH_RADIUS_REAL / EARTH_RADIUS_UNITY;

    public enum ID { LATITUDE, LONGITUDE, ALTITUDE, ORIENTATION };

    public static Vector2 CartesianToSpherical(Vector3 cartesianCoordinates) {
        float theta = 0;
        if (cartesianCoordinates.y > 0) {
            float temp = cartesianCoordinates.x * cartesianCoordinates.x
                + cartesianCoordinates.z * cartesianCoordinates.z;
            theta = Mathf.Atan(Mathf.Sqrt(temp) / cartesianCoordinates.y);
        } else if (cartesianCoordinates.y < 0) {
            float temp = cartesianCoordinates.x * cartesianCoordinates.x
                + cartesianCoordinates.z * cartesianCoordinates.z;
            theta = Mathf.Atan(Mathf.Sqrt(temp) / cartesianCoordinates.y) + Mathf.PI;
        } else if (cartesianCoordinates.y == 0) theta = Mathf.PI / 2;

        float phi = 0;
        if (cartesianCoordinates.x > 0) phi = Mathf.Atan(cartesianCoordinates.z / cartesianCoordinates.x);
        else if (cartesianCoordinates.x < 0 && cartesianCoordinates.z >= 0)
            phi = Mathf.Atan(cartesianCoordinates.z / cartesianCoordinates.x) + Mathf.PI;
        else if (cartesianCoordinates.x < 0 && cartesianCoordinates.z < 0)
            phi = Mathf.Atan(cartesianCoordinates.z / cartesianCoordinates.x) - Mathf.PI;
        else if (cartesianCoordinates.x == 0 && cartesianCoordinates.z > 0) phi = Mathf.PI / 2;
        else if (cartesianCoordinates.x == 0 && cartesianCoordinates.z < 0) phi = -Mathf.PI / 2;

        return new Vector2(theta, phi);
    }

    // Spherical coordinates should be in the format: (theta, phi)
    public static Vector2 SphericalToEarth(Vector2 sphericalCoordinates) {
        float latitude = 90 - (sphericalCoordinates.x * 180 / Mathf.PI);
        float temp = sphericalCoordinates.y + Mathf.PI / 2;
        if (temp > Mathf.PI) temp -= Mathf.PI * 2;
        float longitude = temp * 180 / Mathf.PI;
        return new Vector2(latitude, longitude);
    }

    // Earth coordinates should be in the format: (latitude, longitude, altitude)
    public static Vector3 EarthToSpherical(Vector3 earthCoordinates) {
        float theta = (90 - earthCoordinates.x) * Mathf.PI / 180;
        float temp = earthCoordinates.y * Mathf.PI / 180;
        if (temp < 0) temp += Mathf.PI * 2;
        float phi = temp - Mathf.PI / 2;
        double radius = earthCoordinates.z / REAL_RATIO + EARTH_RADIUS_UNITY;
        return new Vector3(theta, phi, (float) radius);
    }
    public static Vector3 EarthToSpherical(float latitude, float longitude, float altitude) {
        return EarthToSpherical(new Vector3(latitude, longitude, altitude));
    }

    // Spherical coordinates should be in the format: (theta, phi, radius)
    public static Vector3 SphericalToCartesian(Vector3 sphericalCoordinates) {
        float x = sphericalCoordinates.z * Mathf.Sin(sphericalCoordinates.x) * Mathf.Cos(sphericalCoordinates.y);
        float z = sphericalCoordinates.z * Mathf.Sin(sphericalCoordinates.x) * Mathf.Sin(sphericalCoordinates.y);
        float y = sphericalCoordinates.z * Mathf.Cos(sphericalCoordinates.x);
        return new Vector3(x, y, z);
    }
    public static Vector3 SphericalToCartesian(float theta, float phi, float radius) {
        return SphericalToCartesian(new Vector3(theta, phi, radius));
    }
}
