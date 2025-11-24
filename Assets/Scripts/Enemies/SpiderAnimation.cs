using System.Collections.Generic;
using UnityEngine;

namespace Enemies {
    public class SpiderAnimation : MonoBehaviour {
        [Header("Animation Settings")]
        [Tooltip("If legs walks in a synchronized way")]
        [SerializeField] private bool isSynchronedWalking = true;
        [Tooltip("List of legs to animate")]
        [SerializeField] private List<Transform> legs = new();
        [Tooltip("Angle of movement (in degree)")]
        [SerializeField] private float legAngle = 15f;
        [Tooltip("Cycle duration (in second)")]
        [SerializeField] private float legCycleDuration = 1.5f;

        private List<Quaternion> _initialRotations;

        void Start() {
            _initialRotations = new List<Quaternion>();                         // Save legs start rotations
            foreach (var leg in legs) _initialRotations.Add(leg ? leg.localRotation : Quaternion.identity);
        }

        void Update() {
            if (legs.Count == 0) return;

            float time = Time.time;
            for (int i = 0; i < legs.Count; i++) {
                if (!legs[i]) continue;

                float phaseOffset = isSynchronedWalking ? (i / (float)legs.Count) * Mathf.PI * 2 : 
                    Random.Range(0f, Mathf.PI * 2);                             // Way of walking
                float angle = Mathf.Sin((time / legCycleDuration) * Mathf.PI * 2 + phaseOffset) * legAngle;

                legs[i].localRotation = _initialRotations[i] * Quaternion.Euler(angle, angle * 0.5f, 0f);
            }
        }
    }
}
