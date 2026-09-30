using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Скорости")]
    public float walkSpeed = 5f;
    public float crouchSpeed = 2.5f;
    public float runSpeed = 10f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Приседание")]
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchSmooth = 10f;

    [Header("Защита от проваливания")]
    public float maxFallSpeed = -5f;
    public float groundCheckDistance = 0.15f;

    [Header("Слои")]
    [Tooltip("Слои, которые считаются землёй. Исключи слой самого игрока!")]
    public LayerMask groundMask = ~0;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isCrouching;

    // ===== ПУБЛИЧНЫЕ СВОЙСТВА ДЛЯ PlayerAnimation =====
    /// <summary>Стоит ли персонаж на земле (единый источник правды).</summary>
    public bool IsGrounded { get; private set; }
    /// <summary>Вертикальная скорость персонажа.</summary>
    public float VerticalVelocity => velocity.y;
    /// <summary>Приседает ли персонаж прямо сейчас.</summary>
    public bool IsCrouching => isCrouching;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Убираем Rigidbody, если он случайно есть — CharacterController с ним конфликтует
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }

    void Update()
    {
        // ==== ПРОВЕРКА ЗЕМЛИ ====
        Vector3 capsuleBottom = transform.position + controller.center - Vector3.up * (controller.height / 2f);
        Vector3 rayStart = capsuleBottom + Vector3.up * 0.05f;

        bool rayHit = Physics.Raycast(rayStart, Vector3.down, 0.1f + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        IsGrounded = rayHit || controller.isGrounded;

        if (IsGrounded && velocity.y < 0f) velocity.y = -2f;

        // ==== ПРИСЕД ====
        isCrouching = Keyboard.current != null &&
                      (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed);
        HandleCrouch();

        // ==== ГОРИЗОНТАЛЬНОЕ ДВИЖЕНИЕ ====
        float x = 0f, z = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
            if (Keyboard.current.wKey.isPressed) z += 1f;
        }

        Vector3 move = transform.right * x + transform.forward * z;

        bool isRunning = Keyboard.current != null &&
                         (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        float currentSpeed;
        if (isCrouching) currentSpeed = crouchSpeed;
        else if (isRunning) currentSpeed = runSpeed;
        else currentSpeed = walkSpeed;

        controller.Move(move * currentSpeed * Time.deltaTime);

        // ==== ПРЫЖОК ====
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            IsGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // ==== ГРАВИТАЦИЯ ====
        velocity.y += gravity * Time.deltaTime;
        if (velocity.y < maxFallSpeed) velocity.y = maxFallSpeed;

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float targetCenterY = targetHeight / 2f;

        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchSmooth);
        controller.center = new Vector3(
            controller.center.x,
            Mathf.Lerp(controller.center.y, targetCenterY, Time.deltaTime * crouchSmooth),
            controller.center.z
        );
    }
}