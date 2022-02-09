using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace WorldspaceUI.Scripts
{
    public class UseMouseOrXR : MonoBehaviour
    {
        public bool UseMouse;

        private void Awake()
        {
            Camera mainCam = Camera.main;
            if (!mainCam) return;

            WorldSpaceUIDocument doc = FindObjectOfType<WorldSpaceUIDocument>();
            if (!doc) return;
            
            if (UseMouse)
            {
                // move camera out of the XRRig
                mainCam.gameObject.transform.parent = null;
                foreach (var trackedDriver in mainCam.gameObject.GetComponents<TrackedPoseDriver>())
                {
                    trackedDriver.enabled = false;
                }
                
                // Hook up right source
                doc.RaycastSource = FindObjectOfType<MouseRaycastSource>().gameObject;
                foreach (var movementFaker in GetComponents<XRPointerMovementFaker>())
                {
                    movementFaker.enabled = false;
                }

                var xrOrigin = FindObjectOfType<XROrigin>();
                if (xrOrigin)
                {
                    xrOrigin.gameObject.SetActive(false);
                }
            }
        }
    }
}