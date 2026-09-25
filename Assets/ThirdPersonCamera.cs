using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Цель")]
    public Transform target;              // Игрок (volodia)

    [Header("Дистанция")]
    public float distance = 5f;           // Дистанция от игрока
    public float heightOffset = 1.5f;     // Высота точки, на которую смотрит камера

    [Header("Мышь")]
    public float mouseSensitivity = 100f;
    public float minPitch = -30f;         // Насколько вниз можно смотреть
    public float maxPitch = 60f;          // Насколько вверх

    [Header("Плавность")]
    public float smoothSpeed = 15f;       // Плавность следования

    private float yaw = 0f;               // Горизонтальный угол
    private float pitch = 20f;            // Вертикальный угол (по умолчанию смотрим чуть вниз)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (Mouse.current == null) return;

        // === ВВОД МЫШИ ===
        float mouseX = Mouse.current.delta.ReadValue().x * mouseSensitivity * Time.deltaTime;
        float mouseY = Mouse.current.delta.ReadValue().y * mouseSensitivity * Time.deltaTime;

        // Горизонталь — крутим камеру вокруг игрока
        yaw += mouseX;

        // Вертикаль — наклоняем камеру (с ограничениями)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // === ПОЗИЦИЯ КАМЕРЫ ===
        // Точка, вокруг которой вращается камера (уровень головы игрока)
        Vector3 lookPoint = target.position + Vector3.up * heightOffset;

        // Вычисляем позицию камеры на сфере вокруг игрока
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = lookPoint - rotation * Vector3.forward * distance;

        // Плавное перемещение
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Камера смотрит на точку
        transform.LookAt(lookPoint);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}