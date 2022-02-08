using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace WorldspaceUI.Scripts
{
    public class XRPointerMovementFaker : MonoBehaviour
    {
        public XRRayInteractor Interactor;
        private EventSystem _eventSystem;

        private void Awake()
        {
            _eventSystem = FindObjectOfType<EventSystem>();
        }

        private void Update()
        {
            if (!Interactor || !_eventSystem)
            {
                return;
            }

            // simulate pointer moved events
            if (!Interactor.TryGetCurrent3DRaycastHit(out var hit))
            {
                return;
            }

            var eventData = new ExtendedPointerEventData(_eventSystem);

            // what have we hit?
            if (hit.transform)
            {
                eventData.eligibleForClick = false;
                eventData.dragging = false;
                eventData.delta = Vector2.one; // so that IsPointerMoving returns true
                var targetObject = hit.transform.gameObject;
                
                eventData.pointerCurrentRaycast = new RaycastResult
                {
                    gameObject = targetObject,
                    worldPosition = hit.point,
                    distance = hit.distance,
                };
                eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
                var targetPtrHandler = targetObject.GetComponents<IPointerMoveHandler>();
                foreach (var handler in targetPtrHandler)
                {
                    handler.OnPointerMove(eventData);
                }
            }
        }
    }
}