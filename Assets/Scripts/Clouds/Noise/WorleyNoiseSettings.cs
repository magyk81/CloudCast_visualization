using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
//Worley noise, also called Voronoi noise and cellular noise, is a noise function introduced by Steven Worley in 1996.
//Worley noise is an extension of the Voronoi diagram that outputs a real value at a given coordinate that corresponds
//to the Distance of the nth nearest seed, usually nearest seed, and the seeds are distributed evenly through the region.
public class WorleyNoiseSettings : ScriptableObject {

    public int seed;
    [Range (1, 50)]
    public int numDivisionsA = 5;
    [Range (1, 50)]
    public int numDivisionsB = 10;
    [Range (1, 50)]
    public int numDivisionsC = 15;

    public float persistence = .5f;
    public int tile = 1;
    public bool invert = true;

}