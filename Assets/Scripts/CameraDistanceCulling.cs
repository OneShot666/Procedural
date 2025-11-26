using UnityEngine;

// ! Add and use (or found with player) SetCamera()
public class CameraDistanceCulling : MonoBehaviour {
    [Header("Distances par Layer")]
    public float largeObjectsDistance = 200f;
    public float smallObjectsDistance = 50f;

    void Start() {
        Camera cam = GetComponent<Camera>();
        float[] distances = new float[32];                                      // Unity has 32 layers max

        distances[6] = largeObjectsDistance;                                    // Apply distances to layers
        distances[7] = smallObjectsDistance; 

        cam.layerCullDistances = distances;                                     // Unity won't display objects above these distances
    }
}
