using System.Collections.Generic;
using UnityEngine;

// ReSharper disable Unity.InefficientMultidimensionalArrayUsage
namespace Terrain {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour {
        public int chunkSize = 16;
        public int maxHeight = 20;

        [Header("Noise settings")]
        public int seed;
        public int octaves;
        public float noiseScale = 30f;
        public float persistence;
        public float lacunarity;

        [HideInInspector] public Vector2 perlinOffset;

        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;
        private bool[,,] _blocks;

        private readonly Vector3[] _faceDirections = {
            new(0, 0, -1), new(0, 0, 1), new(-1, 0, 0),
            new(1, 0, 0), new(0, 1, 0), new(0, -1, 0)
        };

        private readonly Vector3[,] _faceVertices = {
            { new(0,0,0), new(1,0,0), new(1,1,0), new(0,1,0) },
            { new(1,0,1), new(0,0,1), new(0,1,1), new(1,1,1) },
            { new(0,0,1), new(0,0,0), new(0,1,0), new(0,1,1) },
            { new(1,0,0), new(1,0,1), new(1,1,1), new(1,1,0) },
            { new(0,1,0), new(1,1,0), new(1,1,1), new(0,1,1) },
            { new(0,0,1), new(1,0,1), new(1,0,0), new(0,0,0) }
        };

        void Awake() {
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
        }

        void Start() {
            if (_blocks == null) GenerateBlockData();
        }

        [ContextMenu("Generate Mesh")]
        public void GenerateBlockData() {
            _blocks = new bool[chunkSize, maxHeight, chunkSize];

            for (int x = 0; x < chunkSize; x++) {
                for (int z = 0; z < chunkSize; z++) {
                    float worldX = x + transform.position.x;
                    float worldZ = z + transform.position.z;

                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < octaves; i++) {
                        float sampleX = (worldX + perlinOffset.x + seed) / noiseScale * frequency;
                        float sampleZ = (worldZ + perlinOffset.y + seed) / noiseScale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    float normalizedHeight = (noiseHeight + 1) / 2f;
                    int columnHeight = Mathf.FloorToInt(normalizedHeight * maxHeight);
                    columnHeight = Mathf.Clamp(columnHeight, 1, maxHeight);

                    for (int y = 0; y < columnHeight; y++) _blocks[x, y, z] = true;
                }
            }

            GenerateMesh();
        }
        
        private void GenerateMesh() {
             List<Vector3> verts = new();
             List<Color> colors = new();
             List<Vector2> uvs = new();
             List<int> tris = new();

             int vertexIndex = 0;

             for (int x = 0; x < chunkSize; x++) {
                 for (int y = 0; y < maxHeight; y++) {
                     for (int z = 0; z < chunkSize; z++) {
                         if (!_blocks[x, y, z]) continue;

                         float worldX = x + transform.position.x;
                         float worldZ = z + transform.position.z;
                         Color blockColor = GetBiomeColorAt(worldX, worldZ);

                         for (int f = 0; f < 6; f++) {
                             Vector3 dir = _faceDirections[f];
                             int nx = x + (int)dir.x;
                             int ny = y + (int)dir.y;
                             int nz = z + (int)dir.z;

                             bool faceVisible = nx < 0 || nx >= chunkSize || ny < 0 || ny >= maxHeight ||
                                                nz < 0 || nz >= chunkSize || !_blocks[nx, ny, nz];
                             if (!faceVisible) continue;

                             for (int v = 0; v < 4; v++) {
                                 verts.Add(_faceVertices[f, v] + new Vector3(x, y, z));
                                 colors.Add(blockColor);
                             }

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
                 indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                 vertices = verts.ToArray(), 
                 triangles = tris.ToArray(), 
                 uv = uvs.ToArray(),
                 colors = colors.ToArray()
             };

             mesh.RecalculateNormals();
             
             if (_meshFilter) _meshFilter.mesh = mesh;
             if (_meshCollider) _meshCollider.sharedMesh = mesh;
        }

        private Color GetBiomeColorAt(float x, float z) {
            Color defaultColor = Color.grey; 
            if (ChunkBiomeGenerator.AllBiomes != null)
                foreach (var biome in ChunkBiomeGenerator.AllBiomes)
                    if (biome && biome.IsPointInBiome(x, z))
                        return biome.areaColor;
            return defaultColor;
        }
    }
}
