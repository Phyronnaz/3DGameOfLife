using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeCam : MonoBehaviour
{
    public float speed = 10f;

    public float cursorSensitivity = 0.025f;
    public bool toggleAllowed = true;
    public KeyCode toggleButton = KeyCode.Space;

    public KeyCode sprintButton = KeyCode.LeftShift;
    public float sprintFactor = 5;

    public bool hideCursor = false;

    public bool togglePressed = false;

    private void OnEnable()
    {
        Cursor.visible = !hideCursor;
    }

    private void Update()
    {
        Vector3 deltaPosition = Vector3.zero;

        deltaPosition += transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal");

        var currentSpeed = speed;
        if (Input.GetKey(sprintButton))
        {
            currentSpeed *= sprintFactor;
        }

        transform.position += deltaPosition * currentSpeed * Time.deltaTime;


        if (!togglePressed)
        {
            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.x += -Input.GetAxis("Mouse Y") * 359f * cursorSensitivity;
            eulerAngles.y += Input.GetAxis("Mouse X") * 359f * cursorSensitivity;
            transform.eulerAngles = eulerAngles;
        }


        if (toggleAllowed)
        {
            if (Input.GetKeyDown(toggleButton))
            {
                togglePressed = !togglePressed;
            }
        }
    }
}