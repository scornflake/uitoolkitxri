From:
https://github.com/provencher/MRTK-Quest

With Oculus Link enabled, Unity Crashes when I hit play in editor! What do I do?

If your Quest goes into sleep mode, it will crash the editor when trying to hit play. If the Quest proximity sensor doesn't detect your face, it'll think your Quest isn't used.

Run this via ADB to ensure the Quest is always awake while you develop.

Disable Quest Proximity sensor

adb shell

am broadcast -a com.oculus.vrpowermanager.prox_close

Enable Quest Proximity Sensor

adb shell

am broadcast -a com.oculus.vrpowermanager.automation_disable