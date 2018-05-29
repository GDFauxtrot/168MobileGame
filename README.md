# 168MobileGame

***(Unknown name for now)***

Quick and dirty documentation to get started on Bluetooth (BT) interface (see BluetoothDemo scene for implementation):
 
 - Bluetooth.cs acts as main interface between Java-side BT functions and Unity, via passing strings as function names and Unity "calling" them. Leave this class unmodified unless there *really* needs to be changes made.
 - BluetoothModel.cs provides additional functionality for listening for ("observing") BT events.
   - Any GameObject that wishes to hook into BT must implement IBtObserver interface
   - example: `class ### : MonoBehaviour, IBtObserver { ... }`
 - BluetoothController.cs is just for BluetoothDemo scene, and serves as an example of the use of BT in Unity. It's safe to ignore, the only things that matter are Bluetooth and BluetoothModel.
 
How does multiplayer work?
 - Message sending, ICS167 style. The game should have a state machine tucked into the GameManger or something similar, where clients pass messages back and forth to confirm things, change states, call functions, etc.
 - This means we don't have to jump any odd and unfamiliar hoops to get multiplayer working, fighting against Unet to do simple tasks, and other oddities. Send a message, write a function to read the message and then do a thing. The worst thing we have to deal with is latency at this point.
