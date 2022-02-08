# uitoolkitxri

Geting IPointerMove and general "yes I can touch the UI" working, for UIToolkit, Unity2021.2 and XR/XRI

This takes the work done by kataS:
https://www.reddit.com/r/Unity3D/comments/qh4fe4/here_is_a_script_to_use_uitoolkit_in_runtime/

This adds support for XR/XRI interaction.
Its still a work in progress, but I have basic IPointerMove working.

Yet to ensure everything else is there, but essentially this package:

- Fakes up pointer move events when using XRUIInputModule (it doesnt send them)
- Provides a way to switch out your source of raycasting.  You can have Mouse, or XR.  This is configured manually in the unity editor at the moment.
