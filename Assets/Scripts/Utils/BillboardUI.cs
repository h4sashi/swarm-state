using UnityEngine;

namespace Hanzo.Utils
{


    public class BillboardUI : MonoBehaviour
    {
        private Camera mainCam;

        void Start()
        {
            // Cache the main camera reference
            mainCam = Camera.main;
        }

        void LateUpdate()
        {
            if (mainCam == null) return;

            // Make the UI face the camera directly
            transform.LookAt(transform.position + mainCam.transform.forward);
        }
    }

}
