# Summary

This is a MASSIVE hack that fakes up eventData, and sends it direct to the hit objects
OnPointerMove event handler.

It only works because then the XRRaycastSource just gets the latest hit and returns it.
XRRaycastSource doesn't even LOOK at the eventData (a good thing; cos it's probably wrong)



# Ideally Override. But nooooooooo.

It'd be great if we could just override UIInputModule to get eventData.
Then this would be EASY. Just forward a PointerMoved event with that data.
But no. Everything is f***ing private, protected or internal. And nothing is marked "virtual".
WOULD IT BE SO HARD?!?!?!?  HUH? HUH?

# What does ProcessPointerMovement do, in 2021?

From InputSystemUIInputModule, ProcessPointerMovement:

if eventData.IsPointerMoving, sends:
    ExecuteEvents.Execute(eventData.hovered[i], eventData, ExecuteEvents.pointerMoveHandler);
 
So we need:
- target object (eventData.hovered[i])
  - this'd be the panel object we're trying to hit with the raycast.
  - eventData.hovered seems to have the object hit by the raycast, along with its route to root (upward hierarchy)
- eventData
  - we'll have to fake this. MouseMovement looks to use:
  - control: Vector2Control (Vector2:/Mouse/position)
  - delta: vector2
  - eligibleForClick: false
  - dragging: false
  - pointerCurrentRaycast (RaycastResult)
  - pointerPressRaycast - same as above?
  - pointerEnter: (Worldspace UI Quad; the thing we hit)
  - position: dunno. screenspace? it's a Vec2
  - selectedObject: Worldspace UI Quad
    
- pointerMoveHandler: ExecuteEvents.pointerMoveHandler

#
