using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Камеры")]
    public Camera firstPersonCamera;
    public Camera thirdPersonCamera;
    public GameObject playerModel;

    private bool isFirstPerson = true;

    void Start()
    {
        SwitchCamera(true);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            Debug.Log("Нажата 1 — первое лицо");
            SwitchCamera(true);
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            Debug.Log("Нажата 3 — третье лицо");
            SwitchCamera(false);
        }
    }

    void SwitchCamera(bool firstPerson)
    {
        isFirstPerson = firstPerson;

        if (firstPersonCamera != null)
            firstPersonCamera.gameObject.SetActive(firstPerson);

        if (thirdPersonCamera != null)
            thirdPersonCamera.gameObject.SetActive(!firstPerson);

        if (playerModel != null)
            playerModel.SetActive(!firstPerson);
    }
}