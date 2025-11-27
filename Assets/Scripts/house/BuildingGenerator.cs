using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    // L'énumération pour choisir le type de bâtiment
    public enum BuildingType { Residential, Religious }
    public BuildingType type = BuildingType.Residential;

    [Header("Matériaux")]
    public Material wallMaterial;
    public Material roofMaterial;

    [Header("Dimensions Résidentielles")]
    public float wallWidth = 4f;
    public float wallHeight = 3f;
    public float wallDepth = 4f;
    public float roofPeakHeight = 1.5f;

    // Cette fonction sera appelée par le générateur de centre de population
    // Le [ContextMenu] permet de tester la génération directement dans l'éditeur
    [ContextMenu("Generate Building")]
    public void GenerateBuilding()
    {
        // Nettoyer les objets enfants existants pour éviter les doublons lors de la régénération
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        if (type == BuildingType.Residential)
        {
            GenerateResidentialHouse();
        }
        else if (type == BuildingType.Religious)
        {
            GenerateSimpleChurch();
        }
    }

    private void GenerateResidentialHouse()
    {
        // 1. Murs (Cube)
        GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
        walls.transform.localScale = new Vector3(wallWidth, wallHeight, wallDepth);
        walls.transform.SetParent(this.transform);
        // Positionner pour que la base soit à Y=0
        walls.transform.localPosition = new Vector3(0, wallHeight / 2f, 0);

        // 2. Toit (Cube simple pour le style Low-Poly)
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        float roofOverlap = 0.5f;
        roof.transform.localScale = new Vector3(wallWidth + roofOverlap, roofPeakHeight, wallDepth + roofOverlap);
        roof.transform.SetParent(this.transform);
        // Positionner le toit au-dessus des murs
        float roofYPosition = wallHeight + (roofPeakHeight / 2f);
        roof.transform.localPosition = new Vector3(0, roofYPosition, 0);

        // Appliquer les matériaux
        if (wallMaterial != null) walls.GetComponent<Renderer>().material = wallMaterial;
        if (roofMaterial != null) roof.GetComponent<Renderer>().material = roofMaterial;

        // Nettoyage : retirer les colliders par défaut pour la performance
        Destroy(walls.GetComponent<Collider>());
        Destroy(roof.GetComponent<Collider>());
    }

    private void GenerateSimpleChurch()
    {
        // Définir des tailles plus grandes pour l'église
        float churchWidth = wallWidth * 3.5f;
        float churchHeight = wallHeight * 2f;
        float churchDepth = wallDepth * 2f;
        float towerWidth = 1.5f;
        float towerHeight = churchHeight * 2f;

        // 1. Corps Principal (Nef)
        GameObject mainBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mainBody.transform.localScale = new Vector3(churchWidth, churchHeight, churchDepth);
        mainBody.transform.SetParent(this.transform);
        mainBody.transform.localPosition = new Vector3(0, churchHeight / 2f, 0);

        // 2. Tour / Cloche (Simple Cube)
        GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tower.transform.localScale = new Vector3(towerWidth, towerHeight, towerWidth);
        tower.transform.SetParent(this.transform);
        // Placer la tour à l'avant
        float towerZPos = churchDepth / 2f - towerWidth / 2f;
        tower.transform.localPosition = new Vector3(0, towerHeight / 2f, towerZPos);

        // 3. Toit Principal
        GameObject churchRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        float roofHeight = 1f;
        churchRoof.transform.localScale = new Vector3(churchWidth + 0.5f, roofHeight, churchDepth + 0.5f);
        churchRoof.transform.SetParent(this.transform);
        churchRoof.transform.localPosition = new Vector3(0, churchHeight + roofHeight / 2f, 0);

        // Appliquer les matériaux
        if (wallMaterial != null)
        {
            mainBody.GetComponent<Renderer>().material = wallMaterial;
            tower.GetComponent<Renderer>().material = wallMaterial;
        }
        if (roofMaterial != null) churchRoof.GetComponent<Renderer>().material = roofMaterial;

        // Nettoyage des colliders
        Destroy(mainBody.GetComponent<Collider>());
        Destroy(tower.GetComponent<Collider>());
        Destroy(churchRoof.GetComponent<Collider>());
    }
}
