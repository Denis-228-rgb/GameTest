using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonMovement : MonoBehaviour
{
    public Transform cameraTransform;   // Ссылка на Cam3P (или его корень)
    public float rotationSpeed = 10f;

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        if (cameraTransform == null) return;

        // Если игрок стоит на месте — не крутим
        bool isMoving = Keyboard.current != null &&
            (Keyboard.current.wKey.isPressed ||
             Keyboard.current.aKey.isPressed ||
             Keyboard.current.sKey.isPressed ||
             Keyboard.current.dKey.isPressed);

        if (!isMoving) return;

        // Получаем направление, куда смотрит камера (только по горизонтали)
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        if (cameraForward.sqrMagnitude < 0.01f) return;

        // Плавно поворачиваем игрока в эту сторону
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}