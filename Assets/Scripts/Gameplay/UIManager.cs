using UnityEngine;

namespace Gameplay {
    public class UIManager : MonoBehaviour {
        [Header("UI Reference")]
        public GameObject uiCanvas;

        [Header("Settings")]
        public KeyCode toggleKey = KeyCode.E;
        [HideInInspector] public bool manageCursor;                             // If has control hover mouse

        void Start() {
            if (uiCanvas) uiCanvas.SetActive(true);                             // Show UI by default
        }

        void Update() {
            if (Input.GetKeyDown(toggleKey)) ToggleUI();
        }

        void ToggleUI() {
            if (!uiCanvas) return;

            bool isActive = !uiCanvas.activeSelf;                               // (Un)Show UI
            uiCanvas.SetActive(isActive);

            if (manageCursor) {                                                 // Manage cursor (unused yet)
                if (isActive) {
                    Cursor.lockState = CursorLockMode.None;                     // Unlock and display cursor
                    Cursor.visible = true;
                } else {
                    Cursor.lockState = CursorLockMode.Locked;                   // Hide and lock cursor
                    Cursor.visible = false;
                }
            }
        }
    }
}
