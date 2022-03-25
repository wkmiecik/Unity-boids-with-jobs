using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BoidSettings : ScriptableObject
{
    // Settings
    public float minSpeed = 2;
    public float maxSpeed = 5;
    public float perceptionRadius = 2.5f;
    public float separationRadius = 1f;
    public float maxSteerForce = 3;

    [Header("Collisions")]
    public LayerMask obstacleMask;
    public float avoidRayRadius = 2;
    public float collisionAvoidDst = 5;

    [Header("Weights")]
    public float avoidCollisionWeight = 10;
    public float alignWeight = 1;
    public float cohesionWeight = 1;
    public float seperateWeight = 1;

    [HideInInspector]
    public float perceptionRadiusSqr;
    [HideInInspector]
    public float separationRadiusSqr;


    private void OnValidate()
    {
        perceptionRadiusSqr = perceptionRadius * perceptionRadius;
        separationRadiusSqr = separationRadius * separationRadius;
    }
}