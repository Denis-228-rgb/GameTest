using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Скорости")]
    public float walkSpeed = 5f;
    public float crouchSpeed = 2.5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Приседание")]
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchSmooth = 10f;

    [Header("Защита от проваливания")]
    public float maxFallSpeed = -5f;
    public float groundCheckDistance = 0.15f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }

    void Update()
    {
        Vector3 capsuleBottom = transform.position + controller.center - Vector3.up * (controller.height / 2f);
        Vector3 rayStart = capsuleBottom + Vector3.up * 0.05f;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, 0.1f + groundCheckDistance);

        if (controller.isGrounded) isGrounded = true;

        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        isCrouching = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;
        HandleCrouch();

        float x = 0f;
        float z = 0f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.wKey.isPressed) z += 1f;

        Vector3 move = transform.right * x + transform.forward * z;
        float currentSpeed = isCrouching ? crouchSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

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