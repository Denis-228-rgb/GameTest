using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Продвинутая система анимаций игрока.
/// Управляет состояниями Idle / Walk / Run / Crouch / Jump / Fall / Land.
/// Работает совместно с PlayerMovement.cs (не двигает персонажа, только анимирует).
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class PlayerAnimation : MonoBehaviour
{
    // =========================================================
    //  НАСТРОЙКИ АНИМАЦИИ
    // =========================================================
    [Header("─── Параметры Animator ───")]
    [Tooltip("Имя параметра Float, отвечающего за скорость передвижения")]
    public string speedParameter = "Speed";
    [Tooltip("Имя параметра Bool, отвечающего за приседание")]
    public string crouchParameter = "IsCrouching";
    [Tooltip("Имя параметра Bool, отвечающего за прыжок")]
    public string jumpParameter = "IsJumping";
    [Tooltip("Имя параметра Bool, отвечающего за падение")]
    public string fallParameter = "IsFalling";
    [Tooltip("Имя параметра Trigger для приземления")]
    public string landTriggerParameter = "Land";
    [Tooltip("Имя параметра Bool для бега")]
    public string runParameter = "IsRunning";

    // =========================================================
    //  НАСТРОЙКИ СКОРОСТЕЙ
    // =========================================================
    [Header("─── Значения для Blend Tree ───")]
    [Range(0f, 1f)] public float walkAnimValue = 0.5f;
    [Range(0f, 1f)] public float runAnimValue = 1f;

    [Header("─── Плавность переходов ───")]
    [Tooltip("Скорость разгона анимации (чем больше — тем резче)")]
    public float acceleration = 8f;
    [Tooltip("Скорость торможения анимации")]
    public float deceleration = 12f;

    [Header("─── Пороги состояний ───")]
    [Tooltip("Скорость ниже этого значения считается остановкой")]
    public float moveThreshold = 0.01f;
    [Tooltip("Минимальное время в воздухе, чтобы считать прыжок настоящим")]
    public float minAirTime = 0.1f;
    [Tooltip("Максимальное время падения для срабатывания анимации приземления")]
    public float maxFallTimeForLand = 2f;

    // =========================================================
    //  ССЫЛКИ НА КОМПОНЕНТЫ
    // =========================================================
    private Animator animator;
    private CharacterController controller;

    // =========================================================
    //  ВНУТРЕННЕЕ СОСТОЯНИЕ
    // =========================================================
    private float currentAnimSpeed = 0f;
    private bool isCrouching = false;
    private bool isRunning = false;
    private bool wasGroundedLastFrame = true;
    private float airTime = 0f;
    private float lastLandTime = -10f;

    // Хэши параметров — кэшируем для производительности
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
        controller = GetComponent<CharacterController>();

        // Кэшируем хэши параметров — работает быстрее, чем строки
        speedHash  = Animator.StringToHash(speedParameter);
        crouchHash = Animator.StringToHash(crouchParameter);
        jumpHash   = Animator.StringToHash(jumpParameter);
        fallHash   = Animator.StringToHash(fallParameter);
        landHash   = Animator.StringToHash(landTriggerParameter);
        runHash    = Animator.StringToHash(runParameter);

        // Проверяем, что параметры реально существуют в контроллере — иначе предупреждение
        ValidateParameters();
    }

    /// <summary>
    /// Проверка, что все нужные параметры есть в Animator Controller.
    /// Помогает избежать загадочных "анимация не работает".
    /// </summary>
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
        }
    }

    // =========================================================
    //  ОСНОВНОЙ ЦИКЛ
    // =========================================================
    void Update()
    {
        if (animator == null || controller == null) return;

        // ---- 1. СЧИТЫВАЕМ ВВОД ----
        bool isMoving = ReadMovementInput();
        bool isCrouchingNow = ReadCrouchInput();
        bool isRunningNow = ReadRunInput();

        // ---- 2. ОПРЕДЕЛЯЕМ СОСТОЯНИЕ ЗЕМЛИ ----
        bool isGrounded = CheckGrounded();

        // ---- 3. ОБРАБАТЫВАЕМ ВОЗДУХ ----
        HandleAirState(isGrounded);

        // ---- 4. ОБНОВЛЯЕМ ПАРАМЕТРЫ АНИМАЦИИ ----
        UpdateSpeedParameter(isMoving, isRunningNow, isCrouchingNow);
        UpdateCrouchParameter(isCrouchingNow);
        UpdateRunParameter(isRunningNow, isCrouchingNow);
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

    private bool ReadCrouchInput()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current.leftCtrlKey.isPressed ||
               Keyboard.current.cKey.isPressed;
    }

    private bool ReadRunInput()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current.leftShiftKey.isPressed ||
               Keyboard.current.rightShiftKey.isPressed;
    }

    // =========================================================
    //  ПРОВЕРКА ЗЕМЛИ
    // =========================================================
    private bool CheckGrounded()
    {
        if (controller.isGrounded) return true;

        // Дополнительный Raycast на случай, если isGrounded врёт
        Vector3 rayStart = transform.position + controller.center;
        return Physics.Raycast(rayStart, Vector3.down, controller.height / 2f + 0.15f);
    }

    // =========================================================
    //  СОСТОЯНИЕ В ВОЗДУХЕ (прыжок / падение / приземление)
    // =========================================================
    private void HandleAirState(bool isGrounded)
    {
        // Только что оторвались от земли — считаем время полёта
        if (!isGrounded)
        {
            airTime += Time.deltaTime;
        }

        // Только что приземлились
        if (isGrounded && !wasGroundedLastFrame)
        {
            if (airTime >= minAirTime && airTime <= maxFallTimeForLand)
            {
                // Срабатывает триггер приземления
                animator.SetTrigger(landHash);
                lastLandTime = Time.time;
            }
            airTime = 0f;
        }

        // Управляем параметрами Jump/Fall
        if (isGrounded)
        {
            animator.SetBool(jumpHash, false);
            animator.SetBool(fallHash, false);
        }
        else
        {
            // Если движемся вверх — это прыжок, если вниз — падение
            // (проверяем по вертикальной составляющей через controller.velocity)
            float vertical = controller.velocity.y;
            if (vertical > 0.1f)
            {
                animator.SetBool(jumpHash, true);
                animator.SetBool(fallHash, false);
            }
            else
            {
                animator.SetBool(jumpHash, false);
                animator.SetBool(fallHash, true);
            }
        }

        wasGroundedLastFrame = isGrounded;
    }

    // =========================================================
    //  ОБНОВЛЕНИЕ ПАРАМЕТРА СКОРОСТИ
    // =========================================================
    private void UpdateSpeedParameter(bool isMoving, bool isRunning, bool isCrouchingNow)
    {
        // Целевая скорость анимации
        float targetSpeed = 0f;

        if (isMoving && !isCrouchingNow)
        {
            targetSpeed = isRunning ? runAnimValue : walkAnimValue;
        }
        else if (isMoving && isCrouchingNow)
        {
            // Присед — всегда медленная анимация
            targetSpeed = walkAnimValue * 0.6f;
        }

        // Разгон и торможение идут с разной скоростью — так выглядит естественнее
        float lerpSpeed = targetSpeed > currentAnimSpeed ? acceleration : deceleration;
        currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, targetSpeed, lerpSpeed * Time.deltaTime);

        // Если почти остановились — обнуляем, чтобы не было "дрожания" на минимуме
        if (Mathf.Abs(currentAnimSpeed) < moveThreshold)
            currentAnimSpeed = 0f;

        animator.SetFloat(speedHash, currentAnimSpeed);
    }

    // =========================================================
    //  ОБНОВЛЕНИЕ ПРИСЕДАНИЯ
    // =========================================================
    private void UpdateCrouchParameter(bool isCrouchingNow)
    {
        if (isCrouching == isCrouchingNow) return;

        isCrouching = isCrouchingNow;
        animator.SetBool(crouchHash, isCrouching);
    }

    // =========================================================
    //  ОБНОВЛЕНИЕ БЕГА
    // =========================================================
    private void UpdateRunParameter(bool isRunningNow, bool isCrouchingNow)
    {
        bool shouldRun = isRunningNow && !isCrouchingNow;

        if (isRunning == shouldRun) return;

        isRunning = shouldRun;
        animator.SetBool(runHash, isRunning);
    }

    // =========================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ — можно вызывать из других скриптов
    // =========================================================

    /// <summary>Вернуть текущую скорость анимации (0..1).</summary>
    public float GetCurrentAnimSpeed() => currentAnimSpeed;

    /// <summary>Игрок сейчас в приседе?</summary>
    public bool IsCrouching() => isCrouching;

    /// <summary>Игрок сейчас бежит?</summary>
    public bool IsRunning() => isRunning;

    /// <summary>Игрок сейчас в воздухе?</summary>
    public bool IsInAir() => airTime > minAirTime;

    /// <summary>Сколько времени прошло с последнего приземления.</summary>
    public float TimeSinceLastLand() => Time.time - lastLandTime;

    // =========================================================
    //  ОТЛАДКА
    // =========================================================
#if UNITY_EDITOR
    private void OnGUI()
    {
        // Простой дебаг-оверлей в левом верхнем углу (только в редакторе)
        if (!Application.isPlaying) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.green;

        GUI.Label(new Rect(10, 10, 300, 20),
            $"Anim Speed: {currentAnimSpeed:F2}", style);
        GUI.Label(new Rect(10, 30, 300, 20),
            $"Crouch: {isCrouching} | Run: {isRunning}", style);
        GUI.Label(new Rect(10, 50, 300, 20),
            $"Air Time: {airTime:F2}s", style);
    }
#endif
}