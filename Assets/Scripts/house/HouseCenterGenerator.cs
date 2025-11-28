using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Terrain;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
namespace house {
    [ExecuteInEditMode]
    public class PopulationCentreGenerator : MonoBehaviour {
        [Header("1. Prefabs & Quantity")]
        public BuildingGenerator residentialPrefab;
        public BuildingGenerator churchPrefab;
        public int numberOfResidentialBuildings = 49;

        [Header("2. Zone de Placement")]
        public float areaSize = 80f;
        public float minBuildingSeparation = 7f;

        [Header("3. Reference au Terrain")]
        public BaseTerrainGenerator terrainGenerator;
    
        [Header("4. Ajustements")]
        [Tooltip("Décalage vertical pour poser le bâtiment sur le sol (ex: 1 pour être au-dessus du bloc, 0.5 pour centrer)")]
        public float yOffset = 1f; 

        private readonly List<GameObject> generatedBuildings = new();
        private LayerMask groundLayer;
        private const string GroundLayerName = "Default";

        void Awake() {
            groundLayer = LayerMask.GetMask(GroundLayerName);
        }

        private void Start() {
            if (Application.isPlaying) StartCoroutine(GenerateWhenReadyRoutine());  // Wait until game is launch
        }

        private IEnumerator GenerateWhenReadyRoutine() {
            if (!terrainGenerator) yield break;

            while (!terrainGenerator.IsInitialized) yield return null; 

            GenerateCentre();
        }

        [ContextMenu("Generate Population Centre")]
        public void GenerateCentre() {
            CleanUpPreviousGeneration();

            if (!residentialPrefab || !churchPrefab) return;

            PlaceBuilding(churchPrefab, Vector3.zero, isCentral: true);         // Place church

            for (int i = 0; i < numberOfResidentialBuildings; i++) {            // Place houses
                Vector3 randomPosition;
                int maxAttempts = 50;
                int attempt = 0;

                do {
                    float xPos = Random.Range(-areaSize / 2f, areaSize / 2f);
                    float zPos = Random.Range(-areaSize / 2f, areaSize / 2f);
                
                    xPos = Mathf.Round(xPos);                                   // Align on grid
                    zPos = Mathf.Round(zPos);
                
                    randomPosition = new Vector3(xPos, 0f, zPos);
                    attempt++;
                } while (IsOverlapping(randomPosition) && attempt < maxAttempts);

                if (attempt < maxAttempts) PlaceBuilding(residentialPrefab, randomPosition);
            }
        }

        private void PlaceBuilding(BuildingGenerator prefab, Vector3 localFlatPos, bool isCentral = false) {
            float worldX = transform.position.x + localFlatPos.x;               // Use world position
            float worldZ = transform.position.z + localFlatPos.z;
            float groundHeight = GetHeightAt(worldX, worldZ);                   // Get terrain height
            Vector3 finalWorldPos = new Vector3(worldX, groundHeight + yOffset, worldZ);    // Apply offset

            GameObject newBuildingObj = Instantiate(prefab.gameObject, finalWorldPos, Quaternion.identity, transform);
            newBuildingObj.name = (isCentral ? "Church_CENTRAL" : "Residential_") + generatedBuildings.Count;
            BuildingGenerator buildingScript = newBuildingObj.GetComponent<BuildingGenerator>();

            if (prefab.type == BuildingGenerator.BuildingType.Residential) {
                buildingScript.wallWidth = Random.Range(3f, 6f);
                buildingScript.wallDepth = Random.Range(3f, 6f);
                buildingScript.wallHeight = Random.Range(2.5f, 4f);
            }

            buildingScript.GenerateBuilding();
            generatedBuildings.Add(newBuildingObj);
        }

        private float GetHeightAt(float worldX, float worldZ) {
            if (terrainGenerator) return terrainGenerator.GetHeight(worldX, worldZ);

            if (Physics.Raycast(new Vector3(worldX, 200f, worldZ), Vector3.down, out var hit, 300f, groundLayer))
                return hit.point.y;

            return transform.position.y;                                        // Default height
        }

        private bool IsOverlapping(Vector3 localPos) {
            foreach (GameObject go in generatedBuildings) {
                if (!go) continue;
            
                Vector3 otherLocalPos = go.transform.localPosition;             // Compare local positions
                Vector3 otherPosXZ = new Vector3(otherLocalPos.x, 0, otherLocalPos.z);
            
                float distance = Vector3.Distance(localPos, otherPosXZ);

                if (distance < minBuildingSeparation) return true;
            }
            return false;
        }

        private void CleanUpPreviousGeneration() {
            generatedBuildings.RemoveAll(item => !item);
        
            var tempArray = new List<GameObject>();                             // Clean children
            foreach (Transform child in transform) tempArray.Add(child.gameObject);
            foreach (var child in tempArray) DestroyImmediate(child);
        
            generatedBuildings.Clear();
        }
    }
}
