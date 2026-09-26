using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonMovement : MonoBehaviour
{
    public Transform cameraTransform;
    public float rotationSpeed = 10f;

    void Update()
    {
        // Проверяем: есть ли камера, и включена ли она (активна в Hierarchy)
        if (cameraTransform == null) return;
        if (!cameraTransform.gameObject.activeInHierarchy) return;

        bool isMoving = Keyboard.current != null &&
            (Keyboard.current.wKey.isPressed ||
             Keyboard.current.aKey.isPressed ||
             Keyboard.current.sKey.isPressed ||
             Keyboard.current.dKey.isPressed);

        if (!isMoving) return;

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        if (cameraForward.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}