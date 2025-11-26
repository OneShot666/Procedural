using Random = UnityEngine.Random;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
namespace Terrain {
    [ExecuteAlways]
    public class BiomeGenerator : MonoBehaviour {
        [Header("Terrain Reference")]
        public BaseTerrainGenerator terrain;

        [Header("Biome Settings")]
        public string parentName = "BiomeObjects";
        [Tooltip("Radius or Half-Size of the area.")]
        public float forestSize = 50f;
        [Tooltip("Check this to use a Square shape instead of Circle")]
        public bool useSquareShape;

        [Tooltip("Color of the ground in this biome")]
        public Color areaColor = new(0.4f, 0.7f, 0.2f);                         // Apply color to ground

        [Header("Objects to spawn")]
        public List<GameObject> treePrefabs;

        [Header("Spawning Options")]
        public bool useDensity = true;
        [Range(0, 500)] public int treeCount = 100;
        [Range(0, 100)] public int treeDensityPercent = 50;

        [Header("Tree Offset & Spacing")]
        public float yOffset = -0.5f;
        public float minSpacing = 2f;
        public float border;

        private GameObject _containerObject;
        private static readonly List<BiomeGenerator> AllBiomes = new();
        private readonly List<Vector3> _spawnedPositions = new();
        private bool _isDirty;

        private void OnEnable() {                                               // Save biomes
            if (!AllBiomes.Contains(this)) AllBiomes.Add(this);
        }

        private void OnDisable() {                                              // Remove biomes
            if (AllBiomes.Contains(this)) AllBiomes.Remove(this);
        }

        private void Start() {
            if (Application.isPlaying) GenerateObjects();
        }

        private void Update() {
            if (!Application.isPlaying && _isDirty) {
                GenerateObjects();
                _isDirty = false;
            }
        }

        private void OnValidate() => _isDirty = true;

        /// <summary> Check if position(x, z) inside biome </summary>
        public bool IsPointInBiome(float x, float z) {
            float dx = Mathf.Abs(x - transform.position.x);
            float dz = Mathf.Abs(z - transform.position.z);

            if (useSquareShape) return dx <= forestSize && dz <= forestSize;    // Square biome

            float distSqr = (x - transform.position.x) * (x - transform.position.x) + 
                            (z - transform.position.z) * (z - transform.position.z);
            return distSqr <= forestSize * forestSize;                          // Circle biome
        }

        [ContextMenu("Generate Biome")]
        public void GenerateObjects() {
            if (treePrefabs == null || treePrefabs.Count == 0 || !terrain) return;

            ClearPreviousObjects();
            CreateContainer();
            _spawnedPositions.Clear();

            int count = treeCount;
            if (useDensity) {
                float area = useSquareShape ? forestSize * 2 * (forestSize * 2) // Square area
                    : Mathf.PI * forestSize * forestSize;                       // Circle area
                
                float treeDensity = treeDensityPercent * 0.001f;
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
            GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Count)];
            Vector3 worldPos = Vector3.zero;
            bool validPositionFound = false;

            for (int i = 0; i < 10; i++) {
                Vector2 point;
                float effectiveSize = forestSize - border;

                if (useSquareShape) {                                           // Logic of random position
                    point = new Vector2(
                        Random.Range(-effectiveSize, effectiveSize), 
                        Random.Range(-effectiveSize, effectiveSize)
                    );
                } else point = Random.insideUnitCircle * effectiveSize;
                
                float globalX = transform.position.x + point.x;
                float globalZ = transform.position.z + point.y;

                Vector3 potentialPos = new Vector3(globalX, 0, globalZ);
                if (IsPositionValid(potentialPos)) {
                    float h = terrain.GetHeight((int)globalX, (int)globalZ);
                    potentialPos.y = h + yOffset;
                    worldPos = potentialPos;
                    validPositionFound = true;
                    break;
                }
            }

            if (!validPositionFound) return false;

            _spawnedPositions.Add(worldPos);

            Transform typeParent = _containerObject.transform.Find(prefab.name);
            if (!typeParent) {
                GameObject newParent = new GameObject(prefab.name) { transform = {
                    parent = _containerObject.transform, localPosition = Vector3.zero } };
                typeParent = newParent.transform;
            }

            GameObject obj = Instantiate(prefab, worldPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0f), typeParent);
            obj.SetActive(true); 
            return true;
        }

        private bool IsPositionValid(Vector3 pos) {
            foreach (Vector3 existingPos in _spawnedPositions) {
                float dist = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(existingPos.x, existingPos.z));
                if (dist < minSpacing) return false;
            }
            return true;
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = new Color(areaColor.r, areaColor.g, areaColor.b, 0.3f);  // Use biome's color
            Vector3 center = transform.position;
            
            if (useSquareShape) {                                               // Draw square
                Vector3 size = new Vector3(forestSize * 2, 10f, forestSize * 2); // Height
                Gizmos.DrawCube(center, new Vector3(size.x, 0.1f, size.z));     // Ground
                Gizmos.DrawWireCube(center, size);                              // Limits
            } else {                                                            // Draw circle
                Gizmos.DrawMesh(GetDiscMesh(), center, Quaternion.identity, new Vector3(forestSize * 2, 1, forestSize * 2));
                Gizmos.DrawWireSphere(center, forestSize);
            }
        }

        private Mesh _discMesh;
        private Mesh GetDiscMesh() {
            if (_discMesh) return _discMesh;
            GameObject tempCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _discMesh = tempCylinder.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Destroy(tempCylinder);
            else DestroyImmediate(tempCylinder);
            return _discMesh;
        }
    }
}
