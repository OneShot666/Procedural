using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Collections;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using ToolBuddy;

// Use for IndexFormat
namespace Caves {
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class CaveGenerator : MonoBehaviour {
        [Header("Rendering")]
        [SerializeField] private bool usePhysics = true;
        [SerializeField] private Material caveMaterial;

        [Header("Cave Settings")]
        [SerializeField] private Vector3Int caveSize = new(100, 50, 100);
        [SerializeField, Range(0, 255)] private int densityThreshold = 160;

        [Header("Noise Settings")]
        [SerializeField, Range(0, 0.3f)] private float scale = 0.05f;
        [Tooltip("In milliseconds")]
        [SerializeField, Range(1, 100)] private int frameBudget = 32;
        [SerializeField] private int seed;

        [Header("Follow Player")]
        [Tooltip("Keep caves to fix height")]
        [SerializeField] private bool lockY = true;
        public Transform target;                                                // Player
        [Tooltip("Distance to travel before regenerate")]
        [SerializeField] private float updateDistance = 20f;

        private bool[,,] _mapData;                                              // Stock raw data
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        private readonly List<Vector3> _vertices = new();                       // For mesh construction
        private readonly List<int> _triangles = new();
        private readonly List<Vector2> _uvs = new();

        private Vector3 _lastGenPosition;                                       // To remember last position

        #region Time Slicing & Editor
        private Coroutine _generationCoroutine;

        public void SetTarget(Transform t) => target = t;

        private void EditorUpdate() {
            if (!Application.isPlaying) EditorApplication.QueuePlayerLoopUpdate();
        }

        private void OnEnable() {
            EditorApplication.update += EditorUpdate;
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
            _lastGenPosition = transform.position;
            SetDirty();
        }

        private void OnDisable() {
            EditorApplication.update -= EditorUpdate;
            SetDirty();
        }

        private void OnValidate() {
            caveSize.x = Mathf.Max(1, caveSize.x);
            caveSize.y = Mathf.Max(1, caveSize.y);
            caveSize.z = Mathf.Max(1, caveSize.z);
            SetDirty();
        }
        #endregion

        #region Dirty Handling
        private bool _isDirty;
        private void SetDirty() {
            _isDirty = true;
            if (_generationCoroutine != null) StopCoroutine(_generationCoroutine);
        }

        private void Update() {
            if (Application.isPlaying && target) {
                float dist = Vector2.Distance(                                  // Get hor position
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(target.position.x, target.position.z)
                );

                if (dist > updateDistance) {                                    // Move to player position
                    Vector3 newPos = target.position;
                    if (lockY) newPos.y = _lastGenPosition.y;                   // Keep same height

                    newPos.x = Mathf.Round(newPos.x);                           // Avoid small offsets
                    newPos.y = Mathf.Round(newPos.y);
                    newPos.z = Mathf.Round(newPos.z);

                    transform.position = newPos;
                    SetDirty();                                                 // Regenerate
                }
            }

            if (_isDirty) {
                _lastGenPosition = transform.position;
                _generationCoroutine = StartCoroutine(GenerateTerrainRoutine());
                _isDirty = false;
            }
        }
        #endregion

        private IEnumerator GenerateTerrainRoutine() {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Noise.Seed = seed;
            _mapData = new bool[caveSize.x, caveSize.y, caveSize.z];
            
            if (_meshFilter.sharedMesh) _meshFilter.sharedMesh.Clear();         // Clean scene and variables
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();

            int offsetX = Mathf.RoundToInt(transform.position.x - caveSize.x / 2f);
            int offsetZ = Mathf.RoundToInt(transform.position.z - caveSize.z / 2f);
            int offsetY = Mathf.RoundToInt(transform.position.y);

            for (int x = 0; x < caveSize.x; x++) {                              // Use list of booleans
                for (int y = 0; y < caveSize.y; y++) {
                    for (int z = 0; z < caveSize.z; z++) {
                        int wx = x + offsetX;
                        int wy = offsetY - y;
                        int wz = z + offsetZ;
                        float density = Noise.CalcPixel3D(wx, wy, wz, scale);

                        _mapData[x, y, z] = density < densityThreshold;
                    }
                }
                
                if (stopwatch.ElapsedMilliseconds >= frameBudget) {             // Small pause if operation take too long
                    yield return null;
                    stopwatch.Restart();
                }
            }

            for (int x = 0; x < caveSize.x; x++) {                              // Create mesh
                for (int y = 0; y < caveSize.y; y++) {
                    for (int z = 0; z < caveSize.z; z++) {
                        if (!_mapData[x, y, z]) continue;                       // If isn't block

                        float centeredX = x - caveSize.x / 2f;                  // Center cave position
                        float centeredZ = z - caveSize.z / 2f;
                        Vector3 pos = new Vector3(centeredX, -y, centeredZ);

                        GenerateBlockFaces(x, y, z, pos);                       // Only generate visible faces
                    }
                }

                if (stopwatch.ElapsedMilliseconds >= frameBudget) {
                    yield return null;
                    stopwatch.Restart();
                }
            }

            ApplyMesh();
            _generationCoroutine = null;
        }

        private void GenerateBlockFaces(int x, int y, int z, Vector3 pos) {     // Check 6 neighbors
            // Right, Left, Up, Down, Forward and Backward
            if (IsTransparent(x + 1, y, z)) AddFace(pos, Vector3.right, Vector3.up, Vector3.forward, false);
            if (IsTransparent(x - 1, y, z)) AddFace(pos, Vector3.left, Vector3.up, Vector3.forward, true);
            if (IsTransparent(x, y - 1, z)) AddFace(pos, Vector3.up, Vector3.right, Vector3.forward, true);
            if (IsTransparent(x, y + 1, z)) AddFace(pos, Vector3.down, Vector3.right, Vector3.forward, false);
            if (IsTransparent(x, y, z + 1)) AddFace(pos, Vector3.forward, Vector3.right, Vector3.up, false);
            if (IsTransparent(x, y, z - 1)) AddFace(pos, Vector3.back, Vector3.right, Vector3.up, true);
        }

        private bool IsTransparent(int x, int y, int z) {                       // Transparent if out of list
            if (x < 0 || y < 0 || z < 0 || x >= caveSize.x || y >= caveSize.y || z >= caveSize.z) return true;
            return !_mapData[x, y, z];                                          // Or doesn't exist
        }

        private void AddFace(Vector3 center, Vector3 normal, Vector3 up, Vector3 right, bool reverse) {
            int vIndex = _vertices.Count;
            Vector3 faceCenter = center + normal * 0.5f;                        // Offset to center block
            
            _vertices.Add(faceCenter - right * 0.5f - up * 0.5f);               // Bottom-Left
            _vertices.Add(faceCenter + right * 0.5f - up * 0.5f);               // Bottom-Right
            _vertices.Add(faceCenter + right * 0.5f + up * 0.5f);               // Top-Right
            _vertices.Add(faceCenter - right * 0.5f + up * 0.5f);               // Top-Left

            _uvs.Add(new Vector2(0, 0));
            _uvs.Add(new Vector2(1, 0));
            _uvs.Add(new Vector2(1, 1));
            _uvs.Add(new Vector2(0, 1));

            if (reverse) {
                _triangles.Add(vIndex + 0); _triangles.Add(vIndex + 1); _triangles.Add(vIndex + 2);
                _triangles.Add(vIndex + 0); _triangles.Add(vIndex + 2); _triangles.Add(vIndex + 3);
            } else {
                _triangles.Add(vIndex + 2); _triangles.Add(vIndex + 1); _triangles.Add(vIndex + 0);
                _triangles.Add(vIndex + 3); _triangles.Add(vIndex + 2); _triangles.Add(vIndex + 0);
            }
        }

        private void ApplyMesh() {
            Mesh mesh = new Mesh { indexFormat = IndexFormat.UInt32 };          // Allow more than 65 000 vertex

            mesh.SetVertices(_vertices);
            mesh.SetTriangles(_triangles, 0);
            mesh.SetUVs(0, _uvs);
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _meshFilter.sharedMesh = mesh;
            _meshRenderer.sharedMaterial = caveMaterial;

            if (usePhysics) {
                _meshCollider.sharedMesh = null;                                // Reset to avoid cache error
                _meshCollider.sharedMesh = mesh;
            }
        }
    }
}
