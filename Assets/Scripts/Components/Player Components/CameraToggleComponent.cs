using UnityEngine;

public class CameraToggleComponent : MonoBehaviour
{
    [Header("Referencias")]
    public Camera firstPersonCamera;
    public Camera thirdPersonCamera;

    [Header("Modelo del jugador")]
    public GameObject playerModel;

    [Header("Configuración 3ra Persona")]
    public float thirdPersonDistance = 4.0f;
    public float thirdPersonHeight   = 1.5f;
    public float followSmoothing     = 5.0f;
    public KeyCode toggleKey         = KeyCode.V;

    [Header("Zoom 3ra Persona")]
    public float normalFOV  = 60f;
    public float zoomedFOV  = 30f;
    public float zoomSpeed  = 5f;
    public KeyCode zoomKey  = KeyCode.Mouse1;

    private bool isThirdPerson = false;
    private float targetFOV;

    void Start()
    {
        if (firstPersonCamera == null || thirdPersonCamera == null)
        {
            Debug.LogError("CameraToggleComponent: asigna ambas cámaras en el Inspector.");
            enabled = false;
            return;
        }

        targetFOV = normalFOV;
        thirdPersonCamera.fieldOfView = normalFOV;

        // Estado inicial: 1ra persona
        firstPersonCamera.gameObject.SetActive(true);
        thirdPersonCamera.gameObject.SetActive(false);

        // Ocultar modelo en 1ra persona
        if (playerModel != null)
            playerModel.SetActive(false);
    }

    void Update()
    {
        // Alternar cámaras
        if (Input.GetKeyDown(toggleKey))
        {
            isThirdPerson = !isThirdPerson;

            firstPersonCamera.gameObject.SetActive(!isThirdPerson);
            thirdPersonCamera.gameObject.SetActive(isThirdPerson);

            if (playerModel != null)
                playerModel.SetActive(isThirdPerson);

            // Resetear FOV al cambiar de cámara
            if (!isThirdPerson)
                thirdPersonCamera.fieldOfView = normalFOV;
        }

        // Zoom solo en 3ra persona
        if (isThirdPerson)
        {
            targetFOV = Input.GetKey(zoomKey) ? zoomedFOV : normalFOV;
            thirdPersonCamera.fieldOfView = Mathf.Lerp(
                thirdPersonCamera.fieldOfView,
                targetFOV,
                Time.deltaTime * zoomSpeed
            );
        }
    }

    void LateUpdate()
    {
        if (isThirdPerson)
            UpdateThirdPersonCamera();
    }

    void UpdateThirdPersonCamera()
    {
        Vector3 fpForward = firstPersonCamera.transform.forward;
        fpForward.y = 0f;
        fpForward.Normalize();

        Vector3 targetPosition = transform.position
                               - fpForward * thirdPersonDistance
                               + Vector3.up * thirdPersonHeight;

        thirdPersonCamera.transform.position = Vector3.Lerp(
            thirdPersonCamera.transform.position,
            targetPosition,
            Time.deltaTime * followSmoothing
        );

        thirdPersonCamera.transform.LookAt(
            transform.position + Vector3.up * (thirdPersonHeight * 0.5f)
        );
    }
}