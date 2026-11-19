using UnityEngine;

// Camera system to mimic some aspects of simulation games
public class CameraController : MonoBehaviour
{
    // Create a singleton of the camera controller
    public static CameraController camInstance;

    public Transform camTransform;
    public Transform objectToFollowTransform;

    public float camNormalSpeed;        // For 2D - 0.1, for 3D - 0.5
    public float camFastSpeed;          // For 2D - 0.5, for 3D - 3
    public float camMovementSpeed;      // For 2D - 0.5, for 3D - 1
    public float camMovementTime;       // For 2D - 0.5, for 3D - 5
    //public float camRotationAmount;   // For 3D - 1
    //public Vector3 camZoomAmount;     // For 3D - (0,-5,5)
    public float camZoomAmount;         // For 2D - 5

    public float dragSpeed = 1f;

    private Vector3 camNewPosition;

    // For mouse drag and rotate
    private Vector3 dragOrigin;


    void Awake()
    {
        if(camInstance == null)
        {
            camInstance = this;
        }
    }

    void Start()
    {
        camNewPosition = transform.position;
    }

    // Put LateUpdate() here
    void LateUpdate()
    {
        // Allow for free input from keyboard and mouse
        HandleCameraWithMouseInput();
        HandleCameraMovementInput();
    }

    void HandleCameraWithMouseInput()
    {
        Camera cam = camTransform.GetComponent<Camera>();

        // Zoom using scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (cam.orthographic)
            {
                cam.orthographicSize -= scroll * camZoomAmount;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, 1f, 100f);
            }
            else
            {
                cam.fieldOfView -= scroll * camZoomAmount;
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, 10f, 80f);
            }
        }

        // Right click and drag to move around
        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 diff = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position += diff * dragSpeed;
        }
    }

    void HandleCameraMovementInput()
    {
        // If shift is pressed, move cam faster
        if(Input.GetKey(KeyCode.LeftShift))
        {
            camMovementSpeed = camFastSpeed;
        }
        else
        {
            camMovementSpeed = camNormalSpeed;
        }
        
        // Keep input collection simple
        // Translation of camera
        if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            camNewPosition += (transform.up * camMovementSpeed);
        }
        if(Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            camNewPosition += (transform.up * -camMovementSpeed);
        }
        if(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            camNewPosition += (transform.right * camMovementSpeed);
        }
        if(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            camNewPosition += (transform.right * -camMovementSpeed);
        }
    }
}
