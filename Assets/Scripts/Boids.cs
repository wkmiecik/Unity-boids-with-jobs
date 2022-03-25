using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class Boids : MonoBehaviour
{
    public int boidsCount = 100;
    public BoidSettings settings;
    public GameObject boidPrefab;

    float camSize;

    Boid[] boidsData;
    GameObject[] boidsGameObjects;

    

    public struct Boid 
    {
        public float2 position;
        public float2 velocity;
        public float2 acceleration;

        public float rayRadius;

        public Boid(float2 pos, float2 vel, float rayRadius) {
            this.position = pos;
            this.velocity = vel;
            this.rayRadius = rayRadius;
            this.acceleration = float2.zero;
        }
    }

    void Start()
    {
        camSize = Camera.main.orthographicSize + 0.65f;
        boidsData = new Boid[boidsCount];
        boidsGameObjects = new GameObject[boidsCount];

        // Create boids
        for (int i = 0; i < boidsCount; i++)
        {
            float2 pos = new float2(UnityEngine.Random.Range(-50, 50), UnityEngine.Random.Range(-50, 50));
            float2 vel = math.normalize(new float2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f))) * settings.maxSpeed;
            float rayRadius = UnityEngine.Random.Range(settings.avoidRayRadius, settings.avoidRayRadius + 3);

            Boid newBoid = new Boid(pos, vel, rayRadius);
            GameObject newGO = Instantiate(boidPrefab, new float3(pos.xy, 0), Quaternion.identity);

            boidsData[i] = newBoid;
            boidsGameObjects[i] = newGO;
        }
    }

    void Update()
    {
        for (int i = 0; i < boidsData.Length; i++)
        {
            // Find boids close to this one and apply forces
            int localBoidsCount = 0;
            float2 acceleration = 0;
            float2 avgLocalHeading = 0;
            float2 avgLocalPosition = 0;
            float2 avgAvoidanceHeading = 0;

            for (int j = 0; j < boidsData.Length; j++)
            {
                if (i != j)
                {
                    float distSqr = math.lengthsq(boidsData[j].position - boidsData[i].position);
                    if (distSqr <= settings.perceptionRadiusSqr)
                    {
                        // Local count
                        localBoidsCount++;

                        // Alignment
                        avgLocalHeading += boidsData[j].velocity;

                        // Cohesion
                        avgLocalPosition += boidsData[j].position;

                        // Separation
                        if (distSqr <= settings.separationRadiusSqr)
                        {
                            float2 diff = boidsData[i].position - boidsData[j].position;
                            avgAvoidanceHeading += diff / distSqr;
                        }
                    }
                }
            }


            if (localBoidsCount > 0)
            {
                avgLocalHeading /= localBoidsCount;
                avgLocalPosition /= localBoidsCount;
                avgAvoidanceHeading /= localBoidsCount;

                float2 alignmentForce = SteerTowards(avgLocalHeading, boidsData[i].velocity) * settings.alignWeight;
                float2 cohesionForce = SteerTowards(avgLocalPosition - boidsData[i].position, boidsData[i].velocity) * settings.cohesionWeight;
                float2 seperationForce = SteerTowards(avgAvoidanceHeading, boidsData[i].velocity) * settings.seperateWeight;

                acceleration += alignmentForce;
                acceleration += cohesionForce;
                acceleration += seperationForce;
            }


            //Check for collisions
            int viewResolution = 10;
            for (int j = 0; j < 360 / viewResolution; j++)
            {
                float3 up = boidsGameObjects[i].transform.up;
                int rayRot = j % 2 == 0 ? j * viewResolution : -j * viewResolution;
                float3 collisionAvoidDir = math.mul(quaternion.Euler(0, 0, rayRot), up);
                RaycastHit2D hit = Physics2D.CircleCast(boidsData[i].position, boidsData[i].rayRadius, collisionAvoidDir.xy, settings.collisionAvoidDst, settings.obstacleMask);

                if (hit.collider == null)
                {
                    if (j == 0) break;
                    float2 collisionAvoidForce = SteerTowards(collisionAvoidDir.xy, boidsData[i].velocity) * settings.avoidCollisionWeight;
                    acceleration += collisionAvoidForce;
                    break;
                }
            }


            // Update velocity
            boidsData[i].velocity += acceleration * Time.deltaTime;
            float speed = math.length(boidsData[i].velocity);
            float2 dir = boidsData[i].velocity / speed;
            speed = math.clamp(speed, settings.minSpeed, settings.maxSpeed);
            boidsData[i].velocity = dir * speed;


            // Update position
            float2 vel = boidsData[i].velocity;
            float2 newPos = boidsData[i].position + vel * Time.deltaTime;

            if (newPos.x > camSize) newPos.x = -camSize;
            if (newPos.x < -camSize) newPos.x = camSize;
            if (newPos.y > camSize) newPos.y = -camSize;
            if (newPos.y < -camSize) newPos.y = camSize;
            boidsData[i].position = newPos;
            boidsGameObjects[i].transform.position = new float3(newPos.xy, 0);


            // Update rotation
            vel = math.normalizesafe(vel);
            float angle = math.degrees(math.atan2(vel.y, vel.x)) - 90;
            boidsGameObjects[i].transform.eulerAngles = new float3(0, 0, angle);
        }
    }


    float2 SteerTowards(float2 vector, float2 vel)
    {
        float2 v = math.normalizesafe(vector) * settings.maxSpeed - vel;
        float len = math.length(v);
        return v * math.min(len, settings.maxSteerForce) / len;
    }
}
