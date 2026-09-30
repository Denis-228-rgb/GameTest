using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimation : MonoBehaviour
{
    // =========================================================
    //  НАСТРОЙКИ АНИМАЦИИ
    // =========================================================
    [Header("─── Параметры Animator ───")]
    public string speedParameter = "Speed";
    public string crouchParameter = "IsCrouching";
    public string jumpParameter = "IsJumping";
    public string fallParameter = "IsFalling";
    public string landTriggerParameter = "Land";
    public string runParameter = "IsRunning";

    // =========================================================
    //  НАСТРОЙКИ СКОРОСТЕЙ
    // =========================================================
    [Header("─── Значения для Blend Tree ───")]
    [Range(0f, 1f)] public float walkAnimValue = 0.5f;
    [Range(0f, 1f)] public float runAnimValue = 1f;

    [Header("─── Плавность переходов ───")]
    public float acceleration = 8f;
    public float deceleration = 12f;

    [Header("─── Пороги состояний ───")]
    public float moveThreshold = 0.01f;
    public float minAirTime = 0.1f;
    public float maxFallTimeForLand = 2f;
    public float jumpPriorityTime = 0.2f;

    [Header("─── Отладка ───")]
    public bool showDebug = false;

    // =========================================================
    //  ССЫЛКИ
    // =========================================================
    private Animator animator;
    private PlayerMovement movement;

    // =========================================================
    //  ВНУТРЕННЕЕ СОСТОЯНИЕ
    // =========================================================
    private float currentAnimSpeed = 0f;
    private bool wasGroundedLastFrame = true;
    private float airTime = 0f;

    // Хэши
    private int speedHash;
    private int crouchHash;
    private int jumpHash;
    private int fallHash;
    private int landHash;
    private int runHash;

    // =========================================================
    //  ИНИЦИАЛИЗАЦИЯ
    // =========================================================
    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();

        speedHash  = Animator.StringToHash(speedParameter);
        crouchHash = Animator.StringToHash(crouchParameter);
        jumpHash   = Animator.StringToHash(jumpParameter);
        fallHash   = Animator.StringToHash(fallParameter);
        landHash   = Animator.StringToHash(landTriggerParameter);
        runHash    = Animator.StringToHash(runParameter);

        ValidateParameters();
    }

    private void ValidateParameters()
    {
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[PlayerAnimation] Animator Controller не назначен!");
            return;
        }

        foreach (var p in animator.parameters)
        {
            if (p.name == speedParameter && p.type != AnimatorControllerParameterType.Float)
                Debug.LogWarning($"[PlayerAnimation] Параметр '{speedParameter}' должен быть Float!");
            if (p.name == crouchParameter && p.type != AnimatorControllerParameterType.Bool)
                Debug.LogWarning($"[PlayerAnimation] Параметр '{crouchParameter}' должен быть Bool!");
            if (p.name == jumpParameter && p.type != AnimatorControllerParameterType.Bool)
                Debug.LogWarning($"[PlayerAnimation] Параметр '{jumpParameter}' должен быть Bool!");
            if (p.name == fallParameter && p.type != AnimatorControllerParameterType.Bool)
                Debug.LogWarning($"[PlayerAnimation] Параметр '{fallParameter}' должен быть Bool!");
            if (p.name == runParameter && p.type != AnimatorControllerParameterType.Bool)
                Debug.LogWarning($"[PlayerAnimation] Параметр '{runParameter}' должен быть Bool!");
            if (p.name == landTriggerParameter && p.type != AnimatorControllerParameterType.Trigger)
                Debug.LogWarning($"[PlayerAnimation] Параметр '{landTriggerParameter}' должен быть Trigger!");
        }
    }

    // =========================================================
    //  ОСНОВНОЙ ЦИКЛ — LateUpdate, чтобы отработать ПОСЛЕ PlayerMovement
    // =========================================================
    void LateUpdate()
    {
        if (animator == null || movement == null) return;

        bool isMoving       = ReadMovementInput();
        bool isCrouchingNow = movement.IsCrouching;
        bool isRunningNow   = ReadRunInput();

        bool isGrounded = movement.IsGrounded;
        float vy        = movement.VerticalVelocity;

        HandleAirState(isGrounded, vy);
        UpdateSpeedParameter(isMoving, isRunningNow, isCrouchingNow);
        UpdateCrouchParameter(isCrouchingNow);
        UpdateRunParameter(isRunningNow, isCrouchingNow);

        if (showDebug)
        {
            Debug.Log($"[Anim] grounded={isGrounded} vy={vy:F2} airTime={airTime:F2} " +
                      $"speed={currentAnimSpeed:F2} " +
                      $"J={animator.GetBool(jumpHash)} F={animator.GetBool(fallHash)} " +
                      $"C={animator.GetBool(crouchHash)} R={animator.GetBool(runHash)}");
        }
    }

    // =========================================================
    //  ВОЗДУХ / ПРЫЖОК / ПАДЕНИЕ / ПРИЗЕМЛЕНИЕ
    // =========================================================
    private void HandleAirState(bool isGrounded, float vy)
    {
        if (!isGrounded)
        {
            airTime += Time.deltaTime;

            if (wasGroundedLastFrame)
            {
                // Только что оторвались от земли
                bool isJump = vy > 0.1f;    // положительная вертикальная скорость = прыжок
                animator.SetBool(jumpHash, isJump);
                animator.SetBool(fallHash, !isJump);

                if (showDebug) Debug.Log($"[Anim] Отрыв: {(isJump ? "JUMP" : "FALL")} vy={vy:F2}");
            }
            else if (vy < -0.1f && airTime > jumpPriorityTime)
            {
                // Уже в воздухе и начали падать — переключаемся на Fall
                if (animator.GetBool(jumpHash))
                {
                    animator.SetBool(jumpHash, false);
                    animator.SetBool(fallHash, true);
                    if (showDebug) Debug.Log("[Anim] Jump → Fall");
                }
            }
        }
        else
        {
            if (!wasGroundedLastFrame)
            {
                // Приземление
                animator.SetBool(jumpHash, false);
                animator.SetBool(fallHash, false);

                if (airTime > minAirTime && airTime < maxFallTimeForLand)
                {
                    animator.SetTrigger(landHash);
                    if (showDebug) Debug.Log($"[Anim] LAND trigger (airTime={airTime:F2})");
                }
                else
                {
                    if (showDebug) Debug.Log($"[Anim] Приземление без Land (airTime={airTime:F2})");
                }
            }

            airTime = 0f;
        }

        wasGroundedLastFrame = isGrounded;
    }

    // =========================================================
    //  ПАРАМЕТРЫ СКОРОСТИ / ПРИСЕДА / БЕГА
    // =========================================================
    private void UpdateSpeedParameter(bool isMoving, bool isRunning, bool isCrouching)
    {
        float target = 0f;

        if (isMoving)
        {
            if (isCrouching) target = walkAnimValue * 0.5f;   // в приседе медленно
            else if (isRunning) target = runAnimValue;
            else target = walkAnimValue;
        }

        float rate = target > currentAnimSpeed ? acceleration : deceleration;
        currentAnimSpeed = Mathf.MoveTowards(currentAnimSpeed, target, rate * Time.deltaTime);

        if (currentAnimSpeed < moveThreshold) currentAnimSpeed = 0f;

        animator.SetFloat(speedHash, currentAnimSpeed);
    }

    private void UpdateCrouchParameter(bool isCrouchingNow)
    {
        animator.SetBool(crouchHash, isCrouchingNow);
    }

    private void UpdateRunParameter(bool isRunningNow, bool isCrouchingNow)
    {
        animator.SetBool(runHash, isRunningNow && !isCrouchingNow);
    }

    // =========================================================
    //  ЧТЕНИЕ ВВОДА
    // =========================================================
    private bool ReadMovementInput()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current.wKey.isPressed ||
               Keyboard.current.aKey.isPressed ||
               Keyboard.current.sKey.isPressed ||
               Keyboard.current.dKey.isPressed;
    }

    private bool ReadRunInput()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current.leftShiftKey.isPressed ||
               Keyboard.current.rightShiftKey.isPressed;
    }
}