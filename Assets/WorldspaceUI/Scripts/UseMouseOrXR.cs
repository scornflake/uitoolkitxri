using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using Object = UnityEngine.Object;

namespace WorldspaceUI.Scripts
{
    public class UseMouseOrXR : MonoBehaviour
    {
        public bool UseMouse;

        private void Awake()
        {
            Camera mainCam = Camera.main;
            if (!mainCam) return;

            WorldSpaceUIDocument doc = Object.FindObjectOfType<WorldSpaceUIDocument>();
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
                doc.RaycastSource = Object.FindObjectOfType<MouseRaycastSource>().gameObject;
                foreach (var movementFaker in GetComponents<XRPointerMovementFaker>())
                {
                    movementFaker.enabled = false;
                }

                var xrOrigin = Object.FindObjectOfType<XROrigin>();
                if (xrOrigin)
                {
                    xrOrigin.gameObject.SetActive(false);
                }
            }
        }
    }
}