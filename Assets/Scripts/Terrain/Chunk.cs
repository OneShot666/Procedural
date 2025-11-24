using System.Collections.Generic;
using UnityEngine;

// ReSharper disable Unity.InefficientMultidimensionalArrayUsage
// ReSharper disable Unity.PerformanceCriticalCodeInvocation
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Chunk : MonoBehaviour {                                            // Object of size 16xinfx16
    public int chunkSize = 16;
    public int maxHeight = 20;
    public float noiseScale = 10f;
    
    [HideInInspector] public Vector2 perlinOffset;

    private bool[,,] _blocks;                                                   // 3D grid of chunk
    private MeshFilter _meshFilter;

    private readonly Vector3[] _faceDirections = {
        new(0, 0, -1), new(0, 0, 1), new(-1, 0, 0),                             // Back, Front, Left
        new(1, 0, 0), new(0, 1, 0), new(0, -1, 0)                               // Right, Top, Bottom
    };

    private readonly Vector3[,] _faceVertices = {
        { new(0,0,0), new(1,0,0), new(1,1,0), new(0,1,0) },                     // Back
        { new(1,0,1), new(0,0,1), new(0,1,1), new(1,1,1) },                     // Front
        { new(0,0,1), new(0,0,0), new(0,1,0), new(0,1,1) },                     // Left
        { new(1,0,0), new(1,0,1), new(1,1,1), new(1,1,0) },                     // Right
        { new(0,1,0), new(1,1,0), new(1,1,1), new(0,1,1) },                     // Top
        { new(0,0,1), new(1,0,1), new(1,0,0), new(0,0,0) }                      // Bottom
    };

    void Start() {
        GenerateBlockData();
    }

    [ContextMenu("Generate Mesh")]
    public void GenerateBlockData() {
        _meshFilter = GetComponent<MeshFilter>();
        _blocks = new bool[chunkSize, maxHeight, chunkSize];

        for (int x = 0; x < chunkSize; x++) {
            for (int z = 0; z < chunkSize; z++) {
                float worldX = x + transform.position.x;
                float worldZ = z + transform.position.z;
                float noise = Mathf.PerlinNoise((worldX + perlinOffset.x) / noiseScale, 
                                                (worldZ + perlinOffset.y) / noiseScale);

                int columnHeight = Mathf.FloorToInt(noise * maxHeight);
                columnHeight = Mathf.Clamp(columnHeight, 1, maxHeight);

                for (int y = 0; y < columnHeight; y++) _blocks[x, y, z] = true;
            }
        }
        
        GenerateMesh();
    }

    private void GenerateMesh() {
        List<Vector3> verts = new();
        List<int> tris = new();
        List<Vector2> uvs = new();

        int vertexIndex = 0;

        for (int x = 0; x < chunkSize; x++) {
            for (int y = 0; y < maxHeight; y++) {
                for (int z = 0; z < chunkSize; z++) {
                    if (!_blocks[x, y, z]) continue;

                    // Pour chaque face : visible ?
                    for (int f = 0; f < 6; f++) {
                        Vector3 dir = _faceDirections[f];

                        int nx = x + (int)dir.x;
                        int ny = y + (int)dir.y;
                        int nz = z + (int)dir.z;

                        bool faceVisible = nx < 0 || nx >= chunkSize || ny < 0 || ny >= maxHeight || 
                            nz < 0 || nz >= chunkSize || !_blocks[nx, ny, nz];

                        if (!faceVisible) continue;

                        for (int v = 0; v < 4; v++)                             // Add 4 vertices of face
                            verts.Add(_faceVertices[f, v] + new Vector3(x, y, z));

                        tris.Add(vertexIndex + 0);
                        tris.Add(vertexIndex + 2);
                        tris.Add(vertexIndex + 1);
                        tris.Add(vertexIndex + 0);
                        tris.Add(vertexIndex + 3);
                        tris.Add(vertexIndex + 2);

                        uvs.Add(new Vector2(0, 0));
                        uvs.Add(new Vector2(1, 0));
                        uvs.Add(new Vector2(1, 1));
                        uvs.Add(new Vector2(0, 1));

                        vertexIndex += 4;
                    }
                }
            }
        }

        Mesh mesh = new Mesh {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,             // Support >65k verts
            vertices = verts.ToArray(), triangles = tris.ToArray(),
            uv = uvs.ToArray()
        };

        mesh.RecalculateNormals();
        _meshFilter.mesh = mesh;
    }
}
