using UnityEngine;
using UnityEngine.EventSystems;

namespace WorldspaceUI.Scripts
{
    public class MouseRaycastSource : MonoBehaviour, IRaycastSource
    {
        public bool TryGetCurrentRaycastHit(Camera eventCamera, PointerEventData eventData, out RaycastHit hit)
        {
            hit = default;
            
            // get current event position and create the ray from the event camera
            Vector3 position = eventData.position;
            position.z = 1.0f;
            position = eventCamera.ScreenToWorldPoint(position);

            // make up a plane that has the same position + normal as the panel in worldpspace
            Plane panelPlane = new Plane(transform.forward, transform.position);

            // make a ray, from the camera, in the direction of the click point in worldspace
            // we're aiming for the Plane
            Ray ray = new Ray(eventCamera.transform.position, position - eventCamera.transform.position);

            // Cast ray in worldspace, see if we hit panelPlane
            if (panelPlane.Raycast(ray, out var distance))
            {
                hit = new RaycastHit();
                var directionNormalized = ray.direction.normalized;
                hit.point = ray.origin + distance * directionNormalized;
                hit.normal = directionNormalized;
                return true;
            }

            return false;
        }
    }
}