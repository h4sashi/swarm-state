using UnityEngine;
namespace Hanzo.Utils
{


    public class ScreenShake : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float defaultDuration = 0.2f;
        [SerializeField] private float defaultMagnitude = 0.2f;

        private Vector3 originalPos;
        private float shakeDuration = 0f;
        private float shakeMagnitude = 0.2f;

        void Awake()
        {
            originalPos = transform.localPosition;
        }

        void Update()
        {
            if (shakeDuration > 0)
            {
                // Random offset in a circle
                Vector2 offset = Random.insideUnitCircle * shakeMagnitude;
                transform.localPosition = originalPos + new Vector3(offset.x, offset.y, 0);

                shakeDuration -= Time.deltaTime;
            }
            else
            {
                // Reset when done
                transform.localPosition = originalPos;
            }
        }

        /// <summary>
        /// Trigger a screenshake with custom values
        /// </summary>
        public void TriggerShake(float duration, float magnitude)
        {
            shakeDuration = duration;
            shakeMagnitude = magnitude;
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
