using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
namespace Villages {
    public class BuildingGenerator : MonoBehaviour {
        public enum BuildingType { Residential, Religious }                     // Type of building

        [Header("Buildings")]
        public BuildingType type = BuildingType.Residential;
        public bool hasCollider = true;

        [Header("Materials references")]
        public Material wallMaterial;
        public Material roofMaterial;

        [Header("Dimensions of residential buildings")]
        public float wallWidth = 4f;
        public float wallHeight = 3f;
        public float wallDepth = 4f;
        public float roofPeakHeight = 1.5f;

        [ContextMenu("Generate Building")]
        public void GenerateBuilding() {                                        // Called by VillageGenerator
            foreach (Transform child in transform) DestroyImmediate(child.gameObject);  // Clean children (avoid duplicates)

            if (type == BuildingType.Residential) GenerateResidentialHouse();
            else if (type == BuildingType.Religious) GenerateSimpleChurch();
        }

        private void GenerateResidentialHouse() {
            GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cube);  // Walls are cubes
            walls.transform.localScale = new Vector3(wallWidth, wallHeight, wallDepth);
            walls.transform.SetParent(transform);
            walls.transform.localPosition = new Vector3(0, wallHeight / 2f, 0); // Position base to 0 

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);   // Roofs also are cubes (for now)
            float roofOverlap = 0.5f;
            roof.transform.localScale = new Vector3(wallWidth + roofOverlap, roofPeakHeight, wallDepth + roofOverlap);
            roof.transform.SetParent(transform);
            float roofYPosition = wallHeight + roofPeakHeight / 2f;             // Position roof above walls
            roof.transform.localPosition = new Vector3(0, roofYPosition, 0);

            if (wallMaterial) walls.GetComponent<Renderer>().material = wallMaterial;   // Apply materials
            if (roofMaterial) roof.GetComponent<Renderer>().material = roofMaterial;

            if (!hasCollider) {
                Destroy(walls.GetComponent<Collider>());
                Destroy(roof.GetComponent<Collider>());
            }
        }

        private void GenerateSimpleChurch() {
            float churchWidth = wallWidth * 3.5f;                               // Make church bigger
            float churchHeight = wallHeight * 2f;
            float churchDepth = wallDepth * 2f;
            float towerWidth = 1.5f;
            float towerHeight = churchHeight * 2f;

            GameObject mainBody = GameObject.CreatePrimitive(PrimitiveType.Cube);   // Main body (nef)
            mainBody.transform.localScale = new Vector3(churchWidth, churchHeight, churchDepth);
            mainBody.transform.SetParent(transform);
            mainBody.transform.localPosition = new Vector3(0, churchHeight / 2f, 0);

            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);  // Tower and bell (cubes)
            tower.transform.localScale = new Vector3(towerWidth, towerHeight, towerWidth);
            tower.transform.SetParent(transform);
            float towerZPos = churchDepth / 2f - towerWidth / 2f;               // Place tower before
            tower.transform.localPosition = new Vector3(0, towerHeight / 2f, towerZPos);

            GameObject churchRoof = GameObject.CreatePrimitive(PrimitiveType.Cube); // Roof
            float roofHeight = 1f;
            churchRoof.transform.localScale = new Vector3(churchWidth + 0.5f, roofHeight, churchDepth + 0.5f);
            churchRoof.transform.SetParent(transform);
            churchRoof.transform.localPosition = new Vector3(0, churchHeight + roofHeight / 2f, 0);

            if (wallMaterial) {                                                 // Apply materials
                mainBody.GetComponent<Renderer>().material = wallMaterial;
                tower.GetComponent<Renderer>().material = wallMaterial;
            }
            if (roofMaterial) churchRoof.GetComponent<Renderer>().material = roofMaterial;

            if (!hasCollider) {
                Destroy(mainBody.GetComponent<Collider>());
                Destroy(tower.GetComponent<Collider>());
                Destroy(churchRoof.GetComponent<Collider>());
            }
        }
    }
}
