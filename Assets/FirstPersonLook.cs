using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [Header("Настройки")]
    public Transform playerBody;          // Ссылка на корень игрока (volodia/betmen)
    public float mouseSensitivity = 100f; // Чувствительность мыши

    private float xRotation = 0f;

    void Start()
    {
        // Прячем курсор, чтобы он не вылетал за экран
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // Получаем движение мыши
        float mouseX = Mouse.current.delta.ReadValue().x * mouseSensitivity * Time.deltaTime;
        float mouseY = Mouse.current.delta.ReadValue().y * mouseSensitivity * Time.deltaTime;

        // === ВЕРТИКАЛЬНОЕ ВРАЩЕНИЕ (только камера) ===
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Ограничение: не смотреть сквозь себя
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // === ГОРИЗОНТАЛЬНОЕ ВРАЩЕНИЕ (всё тело игрока) ===
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }

    // Разблокировка курсора по Escape (чтобы можно было выйти из игры)
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}