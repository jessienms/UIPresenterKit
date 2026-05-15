using UnityEngine;
using UnityEngine.InputSystem;

namespace UIPresenterKit.Samples
{
    public sealed class FreeCameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float fastMoveSpeed = 15f;
        [SerializeField] private float lookSensitivity = 0.2f;

        private float yaw;
        private float pitch;

        private void Start()
        {
            var euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null) return;

            // 우클릭 중에만 시점 전환
            if (mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue();
                yaw += delta.x * lookSensitivity;
                pitch -= delta.y * lookSensitivity;
                pitch = Mathf.Clamp(pitch, -89f, 89f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            // WASD + QE 이동 (Shift로 가속)
            var speed = keyboard.leftShiftKey.isPressed ? fastMoveSpeed : moveSpeed;
            var move = Vector3.zero;
            if (keyboard.wKey.isPressed) move += transform.forward;
            if (keyboard.sKey.isPressed) move -= transform.forward;
            if (keyboard.aKey.isPressed) move -= transform.right;
            if (keyboard.dKey.isPressed) move += transform.right;
            if (keyboard.eKey.isPressed) move += Vector3.up;
            if (keyboard.qKey.isPressed) move -= Vector3.up;

            transform.position += move * (speed * Time.deltaTime);
        }
    }
}
