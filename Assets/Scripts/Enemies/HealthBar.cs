using UnityEngine.UI;
using UnityEngine;

namespace Enemies {
    public class HealthBar : MonoBehaviour {
        [SerializeField] private Slider slider;
        [SerializeField] private Vector3 offset = new(0, 2, 0);

        private Transform _cameraTransform;
        private Transform _target;                                                  // Enemy to follow

        void Start() {
            if (Camera.main) _cameraTransform = Camera.main.transform;
        }

        void LateUpdate() {
            if (!_target) return;

            transform.position = _target.position + offset;                     // Follow enemy
            transform.rotation = Quaternion.LookRotation(transform.position - _cameraTransform.position);   // Look at player
        }

        public void Initialize(Transform target, float maxHealth) {
            _target = target;
            slider.value = maxHealth;
            slider.maxValue = maxHealth;
        }

        public void SetHealth(float currentHealth) => slider.value = currentHealth;
    }
}