# [Dragon Trainer](https://dreamworking-dh2413.github.io/website/) 

Dragon Trainer is a co-op VR experience where you and your friend cooperate together as a dragon and its rider to explore a vast landscape, fly through rings, and breathe fire at sheep. Mount our proprietary controller platform and steer the experience together. The dragon controls speed by flapping its wings (your arms) and shoots fire through its mouth. The rider controls pitch with its controller, and together you control the roll by tilting left and right on the platform. By steering and cooperating together, we provide an experience like no other, immersing the players in once-in-a-lifetime fun.

![demo](./img/demo.jpg)
![game](./img/game.png)

## Features
- Immersive dual VR experience
- Multiplayer via Unity network
- Motion tracking using HTC Vive trackers for dragon fly control
- ✨Realistic procedural dragon wing animation
- ✨Realistic procedural infinite terrain generation
- ✨Realistic horizon fog
- Mouth recognition for fire breathing (half working)

## Setup Overview

![setup](./img/setup.png)

## Tech Stack
- Unity 6000.0.62f1 LTS
- SteamVR

## Prerequisites
There are multiple components that are needed in order to provide the full experience of this project:
- Two HTC VIVE headsets, one HTC VIVE Controller, three HTC VIVE trackers, a HTC VIVE room setup with light houses. The controller and the three trackers should connect to a single headset which is the one used for the dragon player. The other headset is only used for displaying the rider's view.
- Two computers that can run the Unity project and connect to the two HTC Vive headsets using SteamVR.
- A board that can tilt left and right as well as a chair that can be mounted on the board (see board setup for details).
- Alternatively a third computer that runs the spectator view.

## Board Setup
The board that was built for our project was made using a table top, soft and medium soft foam, some planks, ratchet straps, and an office chair without the back and arm rests. 

![Board Setup](./img/board_setup.jpg)
(From left to right: full physical setup, bottom of board, top down view of bottom of board)

One of the HTC VIVE trackers should be mounted on the chair, this sensor will keep track of the players tilt angle which will control stearing left and right.

### Mouth recognition for fire breathing
Alternativly (this does not work perfectly and was not used at the open house) you can also set up the camera (Fig 1) and use the OpenCV face recognition script (main.py) before starting the Unity project to breath fire by opening the dragons mouth. You will also need to disable constant firing in the vfx_DragonBreath -> Dragon_Breath game object. In The gameobject FaceDetectionReviecer (Misspelled, I know) you can edit port for the connection with the python script.

## Unity and SteamVR Setup 
Setups that all computers need to follow:
- Make sure all computers are connected to the same local network, or they can be reached via the internet.
- Start the VIVE headset and all the controller and trackers, and pair them to the dragon's headset.
- Connect the headsets to the computers, tell the players to get in position, and calibrate the headsets using SteamVR's setup tool.
- In the Unity project, the "SampleScene" is the main scene.
- The dragon instance of the game should always the one that runs first. It will handle all inputs from the controller and the trackers. 

### Dragon Instance Unity Setup
- After starting the game for the dragon's instance, select the "Dragon" option in view port. This will set the camera to the dragon's perspective and start the hosting for the Unity network.
- Go to the project inspector, find Player -> VRRig, and there are three tracker objects. Select each of them and make sure the device index in the object inpector is the correct one for the left hand, right hand and the board seat.
- The dragon will appear frozen and suspend in the air, which is expected. We will wait until the rider instance to connect before we start the experience. 

### Rider Instance Unity Setup
- Go to the project inspector, select NetworkUI, and change the Host IP to the IP address of the dragon's instance, and make sure the Host Port is set to the same as well.
- After the dragon's instance has started, start the rider's instance of the game, and select the "Rider" option in view port. This will set the camera to the rider's perspective and start the client for the Unity network. Now the game will try to connect to the dragon's instance.

### Spectator Setup (Optional)
- Use another computer to run a third instance of the game without the headset, setup the IP and port the same as the rider instance, and join the game as rider.
- Switch the game view port's display to Display 2.

After the connection is established successfully, the movement and animation of the dragon will be synchronized across all instances. Now press F in the dragon's instance to unfreeze the dragon and the flying experience will start.

If anything would happen during the experience or the players would like to stop, press F again to freeze the dragon.
