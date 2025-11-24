using System.Collections;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using ToolBuddy;

[ExecuteAlways]
public class CaveGenerator : MonoBehaviour {
    [Header("Cave Settings")]
    [SerializeField] private int cubeDimension = 30;

    [Range(0, 255)]
    [SerializeField] private int densityThreshold = 128;

    [Header("Noise Settings")]
    [SerializeField, Range(0, 0.3f)] private float scale = 0.05f;
    [Tooltip("In milliseconds")]
    [SerializeField, Range(1, 100)] private int frameBudget = 32;
    [SerializeField] private int seed;

    [Header("Prefab")]
    [SerializeField] private GameObject cubePrefab;

    #region Time Slicing
    private Coroutine _generationCoroutine;

    private void EditorUpdate() {
        if (!Application.isPlaying) EditorApplication.QueuePlayerLoopUpdate();
    }
    #endregion

    #region Dirtying handling
    private bool _isDirty;

    private void OnEnable() {
        EditorApplication.update += EditorUpdate;
        SetDirty();
    }

    private void OnDisable() {
        EditorApplication.update -= EditorUpdate;
        SetDirty();
    }

    private void OnValidate() {
        SetDirty();
    }

    private void Reset() {
        SetDirty();
    }

    private void SetDirty() {
        _isDirty = true;
        if (_generationCoroutine != null) StopCoroutine(_generationCoroutine);
    }

    private void Update() {
        if (_isDirty) {
            _generationCoroutine = StartCoroutine(GenerateTerrain());
            _isDirty = false;
        }
    }
    #endregion

    private IEnumerator GenerateTerrain() {
        Stopwatch stopwatch = Stopwatch.StartNew();

        DestroyChildren();

        if (!cubePrefab) yield break;

        Noise.Seed = seed;

        float[,,] noiseVolume = Noise.Calc3D( cubeDimension, cubeDimension, cubeDimension, scale );

        for (int x = 0; x < cubeDimension; x++)
            for (int y = 0; y < cubeDimension; y++)
                for (int z = 0; z < cubeDimension; z++) {
                    float density = noiseVolume[x, y, z];

                    if (density >= densityThreshold) continue;

                    Vector3 position = new( x, y, z );
                    GameObject cube = Instantiate( cubePrefab, position, Quaternion.identity, transform );
                    cube.hideFlags = HideFlags.DontSave;

                    if (stopwatch.ElapsedMilliseconds >= frameBudget) {
                        yield return null;
                        stopwatch.Restart();
                    }
                }

        _generationCoroutine = null;
    }

    private void DestroyChildren() {
        for (int i = transform.childCount - 1; i >= 0; i--) {
            Transform child = transform.GetChild(i);
            DestroyImmediate(child.gameObject);
        }
    }
}
