# OmniVest Haptic Vest
A versatile and affordable haptic vest solution focused on use with Virtual Reality headsets.
> **Note**: This project is currently heavily in development and is not fully-functional


## What is This?
This is a project that aims to bring a clean, relatively affordable haptic solution for VR users.
Currently its main focus is for use with VRChat, but plans to create functionality with other games are in the works.
The project is aims to focus on a high level of modularity for the vest (hence the name), allowing for other features to be implemented into the vest relatively easily.
Current features that are planned for the vest include
- Vibration Motors for
  - Chest
  - Arms
  - Legs
- Heart rate sensor.


## Repo Contents
This repo will contain all the files required to build and use your own Omnivest.
- 3D models contains all 3D printable models to be used on the vest.
- Mainboard_Firmware contains the source for the ESP32 firmware.
- OmniVestConnect contains the source for the desktop bridge application.
- PCB Order Files contains the Gerber and BOM files for the PCBs and their components.
- Vibration_Motor_Firmware contains the firmware for each vibration motor controller board.


## The Technicals
The vest runs an I2C bus, meaning that up to 127 different devices can be connected to the bus and function (providing the mainboard has the required code to understand how to handle the device).
Each motor has it's own control board which listens on the I2C bus for a command to change the speed of the motor.
Other devices could easily be connected to the bus and do two-way communication (such as a heart rate monitor).

The mainboard controller (an ESP32-S3) connects to a PC through BluetoothLE, this way information on the states of the modules can be shared between the PC and the vest.
OmniVestConnect bridges the OSC data from an application running on the network to the Bluetooth channel to the vest.


## Please Note
This project is still in very early stages, currently the hardware is untested and a function prototype has not been completed.
There may be some confusion between file names and source code between OmniVest and OmniMod, these mean the same thing. The project has undergone a couple of name changes already so far.

Currently, these parts of the project are in atleast a functional state:
- VRChat avatar contacts
- OmniVestConnect application
- Mainboard Firmware

The rest of the project is still under development and is not yet in a semi-functional state.


## Warranty and Liability
This project does not provide any warranty or take responsibility for any liabilities that may be incurred through the use of the software or hardware.


## Credits
- [OpenShock's "ShockOsc"](https://github.com/OpenShock/ShockOsc) For the original OSC and OSCQuery implementation (this has been modified for OmniVestConnect)
- [LucHeart.CoreOsc](https://www.nuget.org/packages/LucHeart.CoreOSC) For the OSC library
- [InTheHand.BluetoothLE](https://www.nuget.org/packages/InTheHand.BluetoothLE) For the BluetoothLE library
