using UnityEngine;
using System.Collections.Generic;
using MST;
using Terrain; 
using UnityEditor; 

[ExecuteInEditMode]
public class PopulationCentreGenerator : MonoBehaviour
{
    
    [Header("1. Prefabs & Quantité")]
    public BuildingGenerator residentialPrefab;
    public BuildingGenerator churchPrefab;
    public int numberOfResidentialBuildings = 49;

    [Header("2. Zone de Placement")]
    public float areaSize = 80f;
    public float minBuildingSeparation = 7f;

    [Header("3. Référence au Terrain")]
    
    public TerrainGenerator terrainGenerator;

    private List<GameObject> generatedBuildings = new List<GameObject>();
    private LayerMask groundLayer;
    private const string GROUND_LAYER_NAME = "Default";

    void Awake()
    {
        groundLayer = LayerMask.GetMask(GROUND_LAYER_NAME);
        if (groundLayer.value == 0)
        {
            Debug.LogWarning($"Layer '{GROUND_LAYER_NAME}' non trouvé ou pas d'objets sur ce layer. Raycasting pourrait échouer.");
        }
    }

    [ContextMenu("Generate Population Centre")]
    public void GenerateCentre()
    {
        //  NOUVELLE LOGIQUE POUR FORCER LE TERRAIN DANS L'ÉDITEUR 
        if (!Application.isPlaying && terrainGenerator != null)
        {
            Debug.Log("Forçage de la génération du terrain pour le Raycast...");

            // APPEL CLÉ : Exécution de la fonction qui crée les Chunks et leurs Colliders
            terrainGenerator.GenerateTerrain();

            // Forcer l'éditeur à actualiser les composants (surtout les MeshColliders)
            // Cela assure que le Raycast trouve le sol immédiatement.
            EditorUtility.SetDirty(terrainGenerator.gameObject);
        }
       

        CleanUpPreviousGeneration();

        if (residentialPrefab == null || churchPrefab == null)
        {
            Debug.LogError("Veuillez assigner les Prefabs Maison et Église dans l'Inspector.");
            return;
        }

        // 1. Placement de l'Église (Bâtiment Central)
        PlaceBuilding(churchPrefab, Vector3.zero, isCentral: true);

        // 2. Placement des 49 Maisons
        for (int i = 0; i < numberOfResidentialBuildings; i++)
        {
            Vector3 randomPosition;
            int maxAttempts = 50;
            int attempt = 0;

            do
            {
                float xPos = Random.Range(-areaSize / 2f, areaSize / 2f);
                float zPos = Random.Range(-areaSize / 2f, areaSize / 2f);
                randomPosition = new Vector3(xPos, 0f, zPos);

                attempt++;
            } while (IsOverlapping(randomPosition) && attempt < maxAttempts);

            if (attempt < maxAttempts)
            {
                PlaceBuilding(residentialPrefab, randomPosition);
            }
        }
    }

    // --- Logique d'adaptation au Dénivelé et d'Instanciation ---
    private void PlaceBuilding(BuildingGenerator prefab, Vector3 flatPosition, bool isCentral = false)
    {
        // Le Raycast FindGroundHeight va trouver la hauteur du sol (Y)
        float groundHeight = FindGroundHeight(flatPosition);

        // Ajuster la position pour que le bâtiment repose sur le sol
        Vector3 finalPosition = new Vector3(flatPosition.x, groundHeight, flatPosition.z);

        GameObject newBuildingObj = Instantiate(prefab.gameObject, finalPosition, Quaternion.identity, this.transform);
        newBuildingObj.name = (isCentral ? "Church_CENTRAL" : "Residential_") + generatedBuildings.Count;

        BuildingGenerator buildingScript = newBuildingObj.GetComponent<BuildingGenerator>();

        // Variation de taille pour les maisons
        if (prefab.type == BuildingGenerator.BuildingType.Residential)
        {
            buildingScript.wallWidth = Random.Range(3f, 6f);
            buildingScript.wallDepth = Random.Range(3f, 6f);
            buildingScript.wallHeight = Random.Range(2.5f, 4f);
        }

        buildingScript.GenerateBuilding();
        generatedBuildings.Add(newBuildingObj);
    }

    // --- Fonctions d'Utilité ---

    // Raycast pour trouver la hauteur du sol (Y)
    private float FindGroundHeight(Vector3 position)
    {
        RaycastHit hit;
        // Lance le rayon depuis 100 unités au-dessus vers le bas
        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f, groundLayer))
        {
            return hit.point.y;
        }
        // Si le Raycast ne touche RIEN (même après la génération forcée), 
        // nous pouvons utiliser la fonction GetHeight de votre BaseTerrainGenerator comme solution de secours.
        if (terrainGenerator != null)
        {
            // Utiliser l'estimation du bruit Perlin si le collider manque
            return terrainGenerator.GetHeight(position.x, position.z);
        }

        return 0f; // Vraie valeur par défaut
    }

    private bool IsOverlapping(Vector3 position)
    {
        foreach (GameObject go in generatedBuildings)
        {
            Vector3 otherPosXZ = new Vector3(go.transform.position.x, 0, go.transform.position.z);
            float distance = Vector3.Distance(position, otherPosXZ);

            if (distance < minBuildingSeparation)
            {
                return true;
            }
        }
        return false;
    }

    private void CleanUpPreviousGeneration()
    {
        for (int i = generatedBuildings.Count - 1; i >= 0; i--)
        {
            if (generatedBuildings[i] != null)
            {
                DestroyImmediate(generatedBuildings[i]);
            }
        }
        generatedBuildings.Clear();
    }

    public List<GameObject> GetGeneratedBuildings()
    {
        return generatedBuildings;
    }
}