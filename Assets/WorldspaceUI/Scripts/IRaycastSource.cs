using UnityEngine;
using UnityEngine.EventSystems;

namespace WorldspaceUI.Scripts
{
    public interface IRaycastSource 
    {
        public bool TryGetCurrentRaycastHit(Camera eventCamera, PointerEventData eventData, out RaycastHit hit);
    }
}