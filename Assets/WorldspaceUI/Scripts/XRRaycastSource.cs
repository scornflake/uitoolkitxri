using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;

namespace WorldspaceUI.Scripts
{
    public class XRRaycastSource : MonoBehaviour, IRaycastSource
    {
        public LayerMask ValidHitMask;
        public XRRayInteractor RayInteractor;

        public bool TryGetCurrentRaycastHit(Camera eventCamera, PointerEventData eventData, out RaycastHit hit)
        {
            // Just get the latest raycast result, and see if we hit a plane
            return RayInteractor.TryGetCurrent3DRaycastHit(out hit);
        }
    }
}