using UnityEngine;

// ! Put tooltip comments in english
// ! Small collision pb when shot -> had to freeze X & Z rotation for now
namespace Enemies {
    public class GhostAnimation : MonoBehaviour {
        [Header("Floating Settings")]
        [Tooltip("Hauteur moyenne par rapport au sol")]
        [SerializeField] private float hoverHeight = 0.4f;
        [Tooltip("Amplitude de l’oscillation (en mètre)")]
        [SerializeField] private float floatAmplitude = 0.2f;
        [Tooltip("Durée (en secondes) d’un cycle complet de montée/descente")]
        [SerializeField] private float floatCycleDuration = 2f;

        private float _baseY;
        private Rigidbody _rb;

        void Start() {
            _rb = GetComponent<Rigidbody>();
            if (_rb) _baseY = transform.position.y;
        }

        void FixedUpdate() {
            float timeFactor = (Time.time % floatCycleDuration) / floatCycleDuration;
            float sinValue = Mathf.Sin(timeFactor * Mathf.PI * 2);
            float targetY = _baseY + hoverHeight + sinValue * floatAmplitude;

            if (_rb) {                                                          // Only change Y pos
                Vector3 velocity = _rb.linearVelocity;
                float deltaY = (targetY - transform.position.y) / Time.fixedDeltaTime;
                _rb.linearVelocity = new Vector3(velocity.x, deltaY, velocity.z);
            } else transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        }
    }
}
