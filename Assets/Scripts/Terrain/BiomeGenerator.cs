using Random = UnityEngine.Random;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
// ReSharper disable InconsistentNaming
// [UNUSED YET: Replaced by ChunkBiomeGenerator for performance purposes]
namespace Terrain {
    [ExecuteAlways]
    public class BiomeGenerator : MonoBehaviour {
        [Header("Terrain Reference")]
        public BaseTerrainGenerator terrain;

        [Header("Biome Settings")]
        [Tooltip("If checked, objects will be placed inside their chunk as children.")]
        public bool parentedToChunk;
        [Tooltip("The name of the biome to generate")]
        public string parentName = "BiomeObjects";
        [Tooltip("Radius or Half-Size of the area.")]
        public float biomeSize = 50f;
        [Tooltip("Check this to use a Square shape instead of Circle")]
        public bool useSquareShape = true;

        [Tooltip("Color of the ground in this biome")]
        public Color areaColor = new(0.4f, 0.7f, 0.2f);                         // Apply color to ground

        [Header("Objects to spawn")]
        public List<GameObject> objectsPrefabs;

        [Header("Spawning Options")]
        public bool useDensity = true;
        [Range(0, 500)] public int objectNumberMax = 100;
        [Range(0, 100)] public int objectsDensityPercent = 30;

        [Header("Objects Offset & Spacing")]
        public float yOffset;
        public float minSpacing = 2f;
        public float border;

        [Header("Optimization")]
        [Tooltip("Layer to assign to spawned objects")]
        public int objectLayer = 6;                                             // Default layer: 0; Big: 6; Small: 7

        private static readonly List<BiomeGenerator> _allBiomes = new();
        private readonly List<Vector3> _spawnedPositions = new();
        private GameObject _containerObject;
        private Mesh _discMesh;
        private bool _isDirty;

        public static List<BiomeGenerator> AllBiomes => _allBiomes;

        private void Start() {
            if (Application.isPlaying) GenerateObjects();
        }

        private void Update() {
            if (!Application.isPlaying && _isDirty) {
                GenerateObjects();
                _isDirty = false;
            }
        }

        private void OnEnable() {                                               // Save biomes
            if (!_allBiomes.Contains(this)) _allBiomes.Add(this);
        }

        private void OnDisable() {                                              // Remove biomes
            if (_allBiomes.Contains(this)) _allBiomes.Remove(this);
        }

        private void OnValidate() => _isDirty = true;

        private Mesh GetDiscMesh() {
            if (_discMesh) return _discMesh;
            GameObject tempCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _discMesh = tempCylinder.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Destroy(tempCylinder);
            else DestroyImmediate(tempCylinder);
            return _discMesh;
        }

        /// <summary> Check if position(x, z) inside biome </summary>
        public bool IsPointInBiome(float x, float z) {
            float dx = Mathf.Abs(x - transform.position.x);
            float dz = Mathf.Abs(z - transform.position.z);

            if (useSquareShape) return dx <= biomeSize && dz <= biomeSize;    // Square biome

            float distSqr = (x - transform.position.x) * (x - transform.position.x) + 
                            (z - transform.position.z) * (z - transform.position.z);
            return distSqr <= biomeSize * biomeSize;                          // Circle biome
        }

        private bool IsPositionValid(Vector3 pos) {
            foreach (Vector3 existingPos in _spawnedPositions) {
                float dist = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(existingPos.x, existingPos.z));
                if (dist < minSpacing) return false;
            }
            return true;
        }

        [ContextMenu("Generate Biome")]
        public void GenerateObjects() {
            if (objectsPrefabs == null || objectsPrefabs.Count == 0 || !terrain) return;

            ClearPreviousObjects();
            CreateContainer();
            _spawnedPositions.Clear();

            int count = objectNumberMax;
            if (useDensity) {
                float area = useSquareShape ? biomeSize * 2 * (biomeSize * 2) // Square area
                    : Mathf.PI * biomeSize * biomeSize;                       // Circle area
                
                float treeDensity = objectsDensityPercent * 0.001f;
                count = Mathf.RoundToInt(area * treeDensity);
            }

            int maxGlobalAttempts = count * 10; 
            int currentCount = 0;

            for (int i = 0; i < maxGlobalAttempts; i++) {
                if (currentCount >= count) break;
                if (SpawnRandomObject()) currentCount++;
            }
        }

        private void ClearPreviousObjects() {
            Transform previousParent = transform.Find(parentName);
            if (previousParent) {
                if (Application.isPlaying) Destroy(previousParent.gameObject);
                else DestroyImmediate(previousParent.gameObject);
            }
        }

        private void CreateContainer() {
            _containerObject = new GameObject(parentName) { transform = {
                parent = transform, localPosition = Vector3.zero, localRotation = Quaternion.identity } };
        }

        private bool SpawnRandomObject() {
            GameObject prefab = objectsPrefabs[Random.Range(0, objectsPrefabs.Count)];
            Vector3 worldPos = Vector3.zero;
            bool validPositionFound = false;

            for (int i = 0; i < 10; i++) {
                Vector2 point;
                float effectiveSize = biomeSize - border;

                if (useSquareShape) {                                           // Logic of random position
                    point = new Vector2(
                        Random.Range(-effectiveSize, effectiveSize), 
                        Random.Range(-effectiveSize, effectiveSize)
                    );
                } else point = Random.insideUnitCircle * effectiveSize;
                
                float globalX = Mathf.RoundToInt(transform.position.x + point.x);
                float globalZ = Mathf.RoundToInt(transform.position.z + point.y);

                Vector3 potentialPos = new Vector3(globalX, 0, globalZ);
                if (IsPositionValid(potentialPos)) {
                    float h = terrain.GetHeight((int)globalX, (int)globalZ);
                    worldPos = new Vector3(globalX + 0.5f, h + yOffset, globalZ + 0.5f);    // Hor offset to center on block
                    validPositionFound = true;
                    break;
                }
            }

            if (!validPositionFound) return false;

            _spawnedPositions.Add(worldPos);

            GameObject obj = Instantiate(prefab, worldPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0f));

            bool tryToParented = false;
            if (parentedToChunk && terrain is ChunkManager chunkManager) {
                Transform chunkTransform = chunkManager.GetChunkTransform((int) worldPos.x, (int) worldPos.z);
                if (chunkTransform) {
                    obj.transform.SetParent(chunkTransform);
                    tryToParented = true;
                }
            }

            if (!tryToParented) {                                               // If no chunk found
                Transform typeParent = _containerObject.transform.Find(prefab.name);
                if (!typeParent) {
                    GameObject newParent = new GameObject(prefab.name) { transform = {
                        parent = _containerObject.transform, localPosition = Vector3.zero } };
                    typeParent = newParent.transform;
                }

                obj.transform.SetParent(typeParent);
                obj.layer = objectLayer;                                        // Apply layer
                foreach(Transform child in obj.transform) child.gameObject.layer = objectLayer; // Modify layer of children
                obj.SetActive(true);
            }

            return true;
        }

        private void SpawnObjectOnChunk(Chunk chunk, int x, int z) {
            GameObject prefab = objectsPrefabs[Random.Range(0, objectsPrefabs.Count)];

            float h = terrain.GetHeight(x, z);
            Vector3 pos = new Vector3(x + 0.5f, h + yOffset, z + 0.5f);

            GameObject obj = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0f), chunk.transform);

            obj.layer = objectLayer;
            foreach(Transform child in obj.transform) child.gameObject.layer = objectLayer;
        }

        private float RandomHash(int x, int z) {                                // Small better random function
            float v = Mathf.Sin(x * 12.9898f + z * 78.233f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        public void ProcessChunk(Chunk chunk) {
            if (objectsPrefabs == null || objectsPrefabs.Count == 0) return;

            int chunkSize = chunk.chunkSize;
            Vector3 chunkPos = chunk.transform.position;

            for (int x = 0; x < chunkSize; x++) {                               // Among all chunks
                for (int z = 0; z < chunkSize; z++) {
                    int worldX = (int)chunkPos.x + x;
                    int worldZ = (int)chunkPos.z + z;

                    if (IsPointInBiome(worldX, worldZ)) {
                        float randomVal = RandomHash(worldX, worldZ);
                        if (randomVal < objectsDensityPercent / 100f) SpawnObjectOnChunk(chunk, worldX, worldZ);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = new Color(areaColor.r, areaColor.g, areaColor.b, 0.3f);  // Use biome's color
            Vector3 center = transform.position;
            
            if (useSquareShape) {                                               // Draw square
                Vector3 size = new Vector3(biomeSize * 2, 10f, biomeSize * 2);  // Height
                Gizmos.DrawCube(center, new Vector3(size.x, 0.1f, size.z)); // Ground
                Gizmos.DrawWireCube(center, size);                              // Limits
            } else {                                                            // Draw circle
                Gizmos.DrawMesh(GetDiscMesh(), center, Quaternion.identity, new Vector3(biomeSize * 2, 1, biomeSize * 2));
                Gizmos.DrawWireSphere(center, biomeSize);
            }
        }
    }
}
