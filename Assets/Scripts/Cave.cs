using System.Collections.Generic;
using UnityEngine;

// ReSharper disable NotAccessedField.Local
[RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter))]
public class CaveTubeGenerator : MonoBehaviour {
    [Header("Random Seed")]
    [SerializeField] private bool randomSeed = true;
    [SerializeField] private int seed = 12345;

    [Header("Tunnel Path")]
    [SerializeField] private int segmentCount = 200;
    [SerializeField] private float segmentSpacing = 1.5f;
    [SerializeField] private float directionNoise = 0.3f;

    [Header("Tunnel Shape")]
    [SerializeField] private int ringResolution = 12;
    [SerializeField] private float baseRadius = 3f;
    [SerializeField] private float radiusVariation = 1f;

    private void Start() {
        // Random.InitState(seed);
        if (randomSeed) seed = Random.Range(-10000, 10000);

        GenerateTunnel();
    }

    void GenerateTunnel() {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        Vector3 currentPos = transform.position;
        Vector3 direction = transform.forward;
        Vector3 previousNormal = Vector3.up;

        for (int i = 0; i < segmentCount; i++) {
            direction += new Vector3(
                Random.Range(-directionNoise, directionNoise),
                Random.Range(-directionNoise, directionNoise),
                Random.Range(-directionNoise, directionNoise)
            );
            direction.Normalize();
            currentPos += direction * segmentSpacing;

            float radius = baseRadius + Random.Range(-radiusVariation, radiusVariation);

            for (int r = 0; r < ringResolution; r++) {
                float angle = (float)r / ringResolution * Mathf.PI * 2f;

                Vector3 side = Vector3.Cross(direction, previousNormal);
                previousNormal = Vector3.Cross(side, direction);

                Vector3 offset = (Mathf.Cos(angle) * previousNormal + Mathf.Sin(angle) * side) * radius;
                vertices.Add(currentPos + offset);
            }
        }


        for (int i = 0; i < segmentCount - 1; i++) {
            int ringStart = i * ringResolution;
            int nextRingStart = (i + 1) * ringResolution;

            for (int j = 0; j < ringResolution; j++) {
                int a = ringStart + j;
                int b = ringStart + (j + 1) % ringResolution;
                int c = nextRingStart + j;
                int d = nextRingStart + (j + 1) % ringResolution;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
    }
}
