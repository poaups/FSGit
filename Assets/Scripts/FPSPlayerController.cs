using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayerController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField, Range(0.1f, 10f)] private float mouseSensitivity = 1.5f;
    [SerializeField, Range(1f, 89f)] private float maxLookAngle = 85f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float sprintSpeed = 8f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private float verticalVelocity;
    private float pitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (cameraRoot == null && Camera.main != null)
            cameraRoot = Camera.main.transform;
        if (cameraRoot != null && cameraRoot != transform && cameraRoot.IsChildOf(transform))
            pitch = cameraRoot.localEulerAngles.x;
    }

    private void OnEnable()
    {
        var playerMap = InputSystem.actions.FindActionMap("Player");
        if (playerMap == null)
        {
            Debug.LogError("Action map 'Player' introuvable dans les actions Input System.", this);
            return;
        }

        moveAction = playerMap.FindAction("Move");
        lookAction = playerMap.FindAction("Look");
        jumpAction = playerMap.FindAction("Jump");
        sprintAction = playerMap.FindAction("Sprint");
        playerMap.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.actionMap.Disable();
    }

    private void Update()
    {
        if (moveAction == null || lookAction == null || jumpAction == null)
            return;

        Look();
        Move();
    }

    private void Look()
    {
        Vector2 look = lookAction.ReadValue<Vector2>() * mouseSensitivity;
        transform.Rotate(Vector3.up * look.x);
        pitch = Mathf.Clamp(pitch - look.y, -maxLookAngle, maxLookAngle);
        if (cameraRoot != null)
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        float speed = sprintAction != null && sprintAction.IsPressed() ? sprintSpeed : moveSpeed;
        Vector3 horizontal = (transform.right * input.x + transform.forward * input.y) * speed;

        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;
            if (jumpAction.WasPressedThisFrame())
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        characterController.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Cursor.lockState = hasFocus ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !hasFocus;
    }
}

