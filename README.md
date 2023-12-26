# Camera and Character Controller for Unity
A simple character and camera controller that can be used for any 3D project in Unity. This project includes:
- First and third person camera controller
- Character controller for movement, jumping and falling
- Animated character with blended movement animations (Not perfect)
- A basic scene with some obstacles like walls, ramps and stairs

The project uses URP and was created in Unity 2021.3.19f1 version.

## Camera controller
The camera controller is a singleton which controls the behaviour of the first and third person views. Main features included:
- Switch smoothly between first and third person by pressing `C`
- Third person camera collisions with surrounding objects
- Third person view zoom in/out

The key for switching between views can be changed through the inspector.

## Character controller
Like the camera controller, the character controller is also a singleto and includes:
- Walking
- Running
- Jump
- Falling
- Short slowness upon falling

Default key for running is holding down `Shift`, but this can be changed in the inspector. As mentioned before, the character controller also has blended animations when walking in different directions and the character follows the camera direction when moving while in third person view.

## Unity assets
Since the main focus of this project is the programming of these controllers, most of the visual stuff has been taken from the Unity asset store. The list below includes the assets used:
- Basic Motions FREE (Kevin Iglesias)
- DOTween (Demigiant)
- Robot Kyle (Unity Technologies)
- PBR materials from freepbr.com
