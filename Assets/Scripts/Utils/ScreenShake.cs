using UnityEngine;
namespace Hanzo.Utils
{
    // Simple screen shake effect for camera or UI elements 
    public class ScreenShake : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float defaultDuration = 0.2f;
        [SerializeField] private float defaultMagnitude = 0.2f;

        private Vector3 originalPos;
        private float shakeDuration = 0f;
        private float shakeMagnitude = 0.2f;
        private bool isShaking = false;

        void Awake()
        {
            originalPos = transform.localPosition;
        }

        void Update()
        {
            if (isShaking && shakeDuration > 0)
            {
                Vector2 offset = Random.insideUnitCircle * shakeMagnitude;
                transform.localPosition = originalPos + new Vector3(offset.x, offset.y, 0);

                shakeDuration -= Time.deltaTime;
            }
            else if (isShaking) // shaking just ended
            {
                isShaking = false;
                transform.localPosition = originalPos;
            }
        }

        /// <summary>
        /// Trigger a screenshake with custom values
        /// </summary>
        public void TriggerShake(float duration, float magnitude)
        {
            originalPos = transform.localPosition; // cache fresh origin in case camera moved
            shakeDuration = duration;
            shakeMagnitude = magnitude;
            isShaking = true;
        }

        /// <summary>
        /// Trigger a screenshake with default values
        /// </summary>
        public void TriggerShake()
        {
            TriggerShake(defaultDuration, defaultMagnitude);
        }
    }
}