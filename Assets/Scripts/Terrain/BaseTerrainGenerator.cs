using UnityEngine;

namespace Terrain {
    /// <summary> Base of all terrain generator scripts </summary>
    public abstract class BaseTerrainGenerator : MonoBehaviour {
        [Header("General Terrain Settings")]
        [Tooltip("Prefab which contains Chunk.cs script")]
        public GameObject chunkPrefab;
        [Tooltip("Size of chunks")]
        public int chunkSize = 16;
        [Tooltip("Maximum height of relief")]
        public int maxHeight = 30;
        [Tooltip("Noise scale of terrain")]
        public float noiseScale = 30f;
        [Tooltip("Real logic size of area generated")]
        public int mapSize = 100;

        /// <summary> Get height of terrain at point(x,z) </summary>
        public virtual float GetHeight(float x, float z) {
            float nx = x / noiseScale;
            float nz = z / noiseScale;
            return Mathf.PerlinNoise(nx, nz) * maxHeight; 
        }

        /// <summary> Return true if position is already generated (exists and can place entities here) </summary>
        public virtual bool IsPositionGenerated(float x, float z) {
            return x >= 0 && x < mapSize && z >= 0 && z < mapSize;
        }

        /// <summary> Convert world position to chunk coordinates </summary>
        protected Vector2Int WorldToChunkCoord(Vector3 pos) {
            return new Vector2Int(Mathf.FloorToInt(pos.x / chunkSize),
                Mathf.FloorToInt(pos.z / chunkSize));
        }
    }
}
