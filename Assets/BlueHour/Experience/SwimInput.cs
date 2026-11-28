using UnityEngine;
using UnityEngine.InputSystem;

namespace BlueHour.Experience
{
    [RequireComponent(typeof(SwimMotor))]
    public sealed class SwimInput : MonoBehaviour
    {
        private SwimMotor motor;
        private InputAction move;
        private InputAction depth;
        private InputAction look;
        private InputAction capture;
        private InputAction release;
        private bool captured;
        private bool panelOpen;
        public bool Guided { get; set; }
        private bool restoreCapture;

        private void Awake()
        {
            motor = GetComponent<SwimMotor>();
            move = new InputAction("Swim Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");

            depth = new InputAction("Swim Depth", InputActionType.Value);
            depth.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/space")
                .With("Negative", "<Keyboard>/leftCtrl").With("Negative", "<Keyboard>/rightCtrl");

            look = new InputAction("Swim Look", InputActionType.Value, "<Mouse>/delta");
            capture = new InputAction("Capture Pointer", InputActionType.Button, "<Mouse>/leftButton");
            release = new InputAction("Release Pointer", InputActionType.Button, "<Keyboard>/escape");
        }

        private void OnEnable()
        {
            move.Enable();
            depth.Enable();
            look.Enable();
            capture.Enable();
            release.Enable();
        }

        private void Start()
        {
            if (Application.isFocused && !Application.isBatchMode)
                SetCaptured(true);
        }

        public void BlockForPanel(bool blocked)
        {
            if (panelOpen == blocked)
                return;

            panelOpen = blocked;
            if (blocked)
            {
                restoreCapture = captured;
                SetCaptured(false);
            }
            else if (restoreCapture && Application.isFocused && !Application.isBatchMode)
            {
                SetCaptured(true);
            }
        }

        private void Update()
        {
            if (panelOpen)
                return;
            if (release.WasPressedThisFrame())
                SetCaptured(false);
            else if (capture.WasPressedThisFrame() && !captured && Application.isFocused)
            {
                SetCaptured(true);
                return;
            }

            if (captured && Cursor.lockState != CursorLockMode.Locked)
                SetCaptured(false);

            if (captured && !Guided)
                motor.Swim(move.ReadValue<Vector2>(), depth.ReadValue<float>(),
                    look.ReadValue<Vector2>(), Time.deltaTime);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && captured)
                SetCaptured(false);
        }

        private void OnDisable()
        {
            if (captured)
                SetCaptured(false);

            move.Disable();
            depth.Disable();
            look.Disable();
            capture.Disable();
            release.Disable();
        }

        private void OnDestroy()
        {
            move.Dispose();
            depth.Dispose();
            look.Dispose();
            capture.Dispose();
            release.Dispose();
        }

        private void SetCaptured(bool value)
        {
            captured = value;
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;
        }
    }
}
