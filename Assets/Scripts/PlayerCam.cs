using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float sensX = 0.15f;
    public float sensY = 0.15f;
    public Transform orientation;

    float xRotation, yRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;

        UnityEngine.Vector2 d = mouse.delta.ReadValue();
        yRotation += d.x * sensX;
        xRotation -= d.y * sensY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        if (orientation) orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }
}