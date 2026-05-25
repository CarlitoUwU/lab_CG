using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DeathSequenceComponent : MonoBehaviour
{
    [Header("Referencias")]
    public Camera firstPersonCamera;
    public Camera thirdPersonCamera;
    public GameObject playerModel;
    public GameObject gunTransform;
    public Image fadeImage;
    public CameraToggleComponent cameraToggle;

    [Header("Configuración de muerte")]
    public float slowMotionScale = 0.2f;
    public float slowMotionDuration = 1.0f;  // Tiempo en slow motion (real)
    public float fallDuration = 0.8f;  // Duración caída personaje
    public float bodyViewDuration = 2.0f;  // Segundos viendo el cuerpo con cámara retrocediendo
    public float fadeDuration = 1.0f;

    [Header("Caída del arma")]
    public float gunFallSpeed = 3.0f;
    public float gunRotateSpeed = 180.0f;

    [Header("Caída del personaje")]
    public float bodyFallAngle = 85.0f;

    [Header("Cámara retroceso")]
    public float pullbackDistance = 5.0f;  // Cuánto retrocede la cámara
    public float pullbackHeight = 2.0f;  // Altura extra al retroceder

    private FirstPersonPlayerComponent player;
    private DamageableComponent damage;
    private bool sequenceRunning = false;

    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;
    private Vector3 originalGunLocalPos;
    private Quaternion originalGunLocalRot;

    void Start()
    {
        player = GetComponent<FirstPersonPlayerComponent>();
        damage = GetComponent<DamageableComponent>();

        if (damage != null)
            damage.killedDelegates.Register(OnPlayerKilled);

        if (firstPersonCamera != null)
        {
            originalCameraLocalPos = firstPersonCamera.transform.localPosition;
            originalCameraLocalRot = firstPersonCamera.transform.localRotation;
        }
        if (gunTransform != null)
        {
            originalGunLocalPos = gunTransform.transform.localPosition;
            originalGunLocalRot = gunTransform.transform.localRotation;
        }
    }

    void OnPlayerKilled(DamageableComponent dmg)
    {
        if (!sequenceRunning)
            StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        sequenceRunning = true;

        // PASO 1: Slow motion
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Asegurar 1ra persona
        bool wasThirdPerson = thirdPersonCamera != null && thirdPersonCamera.gameObject.activeSelf;
        if (wasThirdPerson)
        {
            firstPersonCamera.gameObject.SetActive(true);
            thirdPersonCamera.gameObject.SetActive(false);
            if (playerModel != null) playerModel.SetActive(false);
        }

        // PASO 2: Caída del arma y cámara simultáneas
        StartCoroutine(GunFall());
        yield return StartCoroutine(CameraFall());

        // PASO 3: Cambiar a 3ra persona y retroceder cámara
        if (thirdPersonCamera != null)
        {
            firstPersonCamera.gameObject.SetActive(false);
            thirdPersonCamera.gameObject.SetActive(true);

            if (playerModel != null)
            {
                playerModel.SetActive(true);
                playerModel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        // PASO 4: Cámara retrocede lentamente mostrando el escenario por bodyViewDuration segundos
        yield return StartCoroutine(CameraPullback());

        // PASO 5: Fade a negro
        yield return StartCoroutine(FadeToBlack());

        // PASO 6: Restaurar tiempo ANTES de que FirstPersonPlayerComponent haga el respawn
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // PASO 7: Restaurar modelo y cámaras
        if (playerModel != null)
            playerModel.transform.localRotation = Quaternion.identity;

        firstPersonCamera.gameObject.SetActive(true);
        if (thirdPersonCamera != null)
            thirdPersonCamera.gameObject.SetActive(false);
        if (playerModel != null)
            playerModel.SetActive(false);

        firstPersonCamera.transform.localPosition = originalCameraLocalPos;
        firstPersonCamera.transform.localRotation = originalCameraLocalRot;

        if (gunTransform != null)
        {
            gunTransform.transform.localPosition = originalGunLocalPos;
            gunTransform.transform.localRotation = originalGunLocalRot;
        }

        // PASO 8: Esperar a que FirstPersonPlayerComponent mueva al jugador al punto de respawn
        // PlayerRespawnVolumeComponent ya tiene la posición correcta
        yield return new WaitForSecondsRealtime(0.1f);

        // PASO 9: Fade desde negro
        yield return StartCoroutine(FadeFromBlack());

        sequenceRunning = false;
    }

    IEnumerator GunFall()
    {
        if (gunTransform == null) yield break;

        float elapsed = 0f;
        Vector3 startPos = gunTransform.transform.localPosition;
        Vector3 targetPos = startPos + Vector3.down * 0.5f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fallDuration;
            gunTransform.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            gunTransform.transform.Rotate(Vector3.right, gunRotateSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
    }

    IEnumerator CameraFall()
    {
        if (firstPersonCamera == null) yield break;

        float elapsed = 0f;
        Quaternion startRot = firstPersonCamera.transform.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(bodyFallAngle, 0f, 15f);
        Vector3 startPos = firstPersonCamera.transform.localPosition;
        Vector3 targetPos = startPos + Vector3.down * 0.8f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fallDuration);
            firstPersonCamera.transform.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            firstPersonCamera.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
    }

    IEnumerator CameraPullback()
    {
        if (thirdPersonCamera == null) yield break;

        float elapsed = 0f;

        // Posición inicial: sobre el cuerpo
        Vector3 startPos = transform.position + Vector3.up * 1.5f;
        thirdPersonCamera.transform.position = startPos;
        thirdPersonCamera.transform.LookAt(transform.position);

        // Posición final: retrocedida y elevada
        Vector3 back = -firstPersonCamera.transform.forward;
        back.y = 0f;
        back.Normalize();
        Vector3 endPos = transform.position
                       + back * pullbackDistance
                       + Vector3.up * (1.5f + pullbackHeight);

        while (elapsed < bodyViewDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / bodyViewDuration);

            thirdPersonCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            thirdPersonCamera.transform.LookAt(
                transform.position + Vector3.up * 0.3f
            );

            yield return null;
        }
    }

    IEnumerator FadeToBlack()
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, 1f);
    }

    IEnumerator FadeFromBlack()
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeImage.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
    }
}