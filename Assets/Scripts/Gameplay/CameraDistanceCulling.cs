using UnityEngine;

namespace Gameplay {
    public class CameraDistanceCulling : MonoBehaviour {
        [Header("Configuration of layers")]
        public int largeObjectsLayer = 6; 
        public int smallObjectsLayer = 7;

        [Header("Distances of display")]
        public float largeObjectsDistance = 200f;
        public float smallObjectsDistance = 50f;

        /// <summary> Called by EntitySpawnManager once player is created </summary>
        public void SetupCamera(Camera playerCamera) {
            if (!playerCamera) return;

            float[] distances = new float[32];                                  // Unity manage 32 layers maximum

            distances[smallObjectsLayer] = smallObjectsDistance;                // Only change specific layers (0 by default)
            distances[largeObjectsLayer] = largeObjectsDistance;

            playerCamera.layerCullDistances = distances;                        // Apply distances to camera
        }
    }
}
