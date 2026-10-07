using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Déplacement et vue à la première personne (Input System + CharacterController).
/// ZQSD/WASD + souris, Shift pour courir, Espace pour sauter, Échap pour libérer la souris.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FPSPlayerController : MonoBehaviour
{
    // Le delta souris de l'Input System est en pixels : on le convertit en degrés.
    private const float MouseDegreesPerPixel = 0.1f;

    [Header("Camera")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField, Range(0.1f, 10f)] private float mouseSensitivity = 1.5f;
    [SerializeField, Min(1f)] private float gamepadLookSpeed = 140f;
    [SerializeField, Range(1f, 89f)] private float maxLookAngle = 85f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float sprintSpeed = 8f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField, Min(0f)] private float groundAcceleration = 60f;
    [SerializeField, Min(0f)] private float airAcceleration = 15f;

    private CharacterController characterController;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float pitch;
    private int cursorLockFrame = -1;

    /// <summary>Vrai si la souris est capturée par le jeu (les armes ne tirent que dans ce cas).</summary>
    public bool CursorCaptured => Cursor.lockState == CursorLockMode.Locked && Time.frameCount != cursorLockFrame;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (cameraRoot == null && Camera.main != null)
            cameraRoot = Camera.main.transform;
        if (cameraRoot != null && cameraRoot != transform && cameraRoot.IsChildOf(transform))
            pitch = Mathf.DeltaAngle(0f, cameraRoot.localEulerAngles.x);
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

    private void Start()
    {
        SetCursorLocked(true);
    }

    private void Update()
    {
        if (moveAction == null || lookAction == null || jumpAction == null)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            // Souris libérée (Échap) : un clic dans la fenêtre reprend le contrôle.
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                SetCursorLocked(true);
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
            return;
        }

        Look();
        Move();
    }

    /// <summary>Fait monter la visée (recul de l'arme). Valeurs en degrés.</summary>
    public void AddRecoil(float pitchUp, float yawRight)
    {
        pitch = Mathf.Clamp(pitch - pitchUp, -maxLookAngle, maxLookAngle);
        transform.Rotate(Vector3.up * yawRight);
        ApplyPitch();
    }

    private void Look()
    {
        Vector2 raw = lookAction.ReadValue<Vector2>();
        bool usingGamepad = lookAction.activeControl != null && lookAction.activeControl.device is Gamepad;
        Vector2 look = usingGamepad
            ? raw * (gamepadLookSpeed * Time.deltaTime)
            : raw * (mouseSensitivity * MouseDegreesPerPixel);

        transform.Rotate(Vector3.up * look.x);
        pitch = Mathf.Clamp(pitch - look.y, -maxLookAngle, maxLookAngle);
        ApplyPitch();
    }

    private void ApplyPitch()
    {
        if (cameraRoot != null)
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        bool grounded = characterController.isGrounded;
        float speed = sprintAction != null && sprintAction.IsPressed() ? sprintSpeed : moveSpeed;
        Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * speed;

        float acceleration = grounded ? groundAcceleration : airAcceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

        if (grounded)
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

        characterController.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        // Plafond : on coupe la montée pour ne pas rester "collé" sous un obstacle.
        if ((characterController.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        if (locked)
            cursorLockFrame = Time.frameCount;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            SetCursorLocked(true);
        else
            SetCursorLocked(false);
    }
}
