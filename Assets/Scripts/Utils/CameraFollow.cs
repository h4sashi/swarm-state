using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 6f, -6f);
    [SerializeField] private float followSpeed = 8f;

    [Header("Rotation Settings")]
    [SerializeField] private float yawSpeed = 120f;   // Left/right rotation speed
    [SerializeField] private float pitchSpeed = 80f;  // Up/down rotation speed
    [SerializeField] private float minPitch = 15f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Auto-Rotate Behind Player")]
    [SerializeField] private bool autoRotateBehindPlayer = true;
    [SerializeField] private float rotateBehindLerpSpeed = 5f;

    [Header("Look Ahead Settings")]
    [SerializeField] private bool enableLookAhead = true;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSmooth = 5f;

    [Header("Smoothing")]
    [SerializeField] private float rotationSmoothTime = 0.12f;

    private float currentYaw;
    private float currentPitch = 30f; // starting pitch
    private Vector3 currentVelocity;

    private Vector3 smoothRotationVelocity;
    private Quaternion smoothRotation;

    private Vector3 lastPlayerPos;
    private Vector3 lookAheadOffset; // smoothed look ahead target

    void Start()
    {
        if (target)
            lastPlayerPos = target.position;
    }

    void LateUpdate()
    {
        if (!target) return;

        // --- Auto rotate behind player if enabled ---
        if (autoRotateBehindPlayer)
        {
            Vector3 playerMovement = target.position - lastPlayerPos;
            if (playerMovement.sqrMagnitude > 0.001f) // if moving
            {
                // Get forward angle of player movement in world space
                float moveYaw = Mathf.Atan2(playerMovement.x, playerMovement.z) * Mathf.Rad2Deg;
                currentYaw = Mathf.LerpAngle(currentYaw, moveYaw, rotateBehindLerpSpeed * Time.deltaTime);
            }
        }

        // Smooth rotation
        Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        smoothRotation = Quaternion.Lerp(smoothRotation, targetRotation, 1 - Mathf.Exp(-rotationSmoothTime * Time.deltaTime));

        // Camera position
        Vector3 desiredPosition = target.position + smoothRotation * offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / followSpeed);

        // --- Look Ahead Calculation ---
        Vector3 velocity = (target.position - lastPlayerPos) / Time.deltaTime;
        Vector3 desiredLookAhead = Vector3.zero;
        if (enableLookAhead && velocity.sqrMagnitude > 0.001f)
        {
            desiredLookAhead = velocity.normalized * lookAheadDistance;
        }
        lookAheadOffset = Vector3.Lerp(lookAheadOffset, desiredLookAhead, Time.deltaTime * lookAheadSmooth);

        // Look at target + look ahead
        transform.LookAt(target.position + Vector3.up * 1.5f + lookAheadOffset);

        // Store last player pos
        lastPlayerPos = target.position;
    }
}
