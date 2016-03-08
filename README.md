# Raspberry Pi 3 Closed-Loop Control Demo

This project showcases how to use Windows 10 IoT Core on a Raspberry Pi 3 to spin a motor by providing input to a motor controller and then measuring the speed of the spinning motor using a digital encoder. We use the digital encoder feedback in a PID loop to control the speed of the motor and by doing this, created a closed loop control system!

Aside from that, it demonstrates the Windows 10 IoT Core's capabilities to provide and interact with a Remote UI and sensors.

**NOTE: The demo code in this repository uses certain Windows IoT Core features that are not yet available. This notice will be removed as soon as all dependent features have been released.**

## Get the Code
This repository contains references to submodules. Be sure to use the --recursive flag when cloning.

```git clone --recursive https://github.com/ms-iot/pid-control-system.git```

## Hardware Build
For information on the hardware build see [our post on Hackster.io](https://www.hackster.io/windows-iot/closed-loop-control-remote-sensors-and-remote-ux-on-rpi3-ef3ed0).

## DemoApp
This project contains the main UI for the demo and includes the Motor and PidController projects. The demo can run in two modes. In throttle mode the slider will adjust the throttle of the motor between 0 and 100%. While the motor controllers used are capable of driving the motor in reverse, this project doesn't use that functionality but it can easily be acheieved by setting a negative percentage as the motor throttle. In closed-loop mode the application will control the motor throttle to achieve the desired RPM which is again set with the slider control.

## Motor
This project contains the logic for getting the RPM of the motor shaft and controlling the motor throttle. Throttle is controlled using a PWM-controlled motor controller which is attached to an Adafruit PWM Servo Hat. The RPM value is calculated using [Lightning](https://ms-iot.github.io/content/en-US/win10/LightningProviders.htm) and an optical encoder. Knowing the pulses per revolution value of the encoder along with the elapsed time between interrupts we can easily calculate the RPM. This results in the RPM being calculated hundreds of times a second which can lead to a very noisy output so we used an [exponential moving average](https://en.wikipedia.org/wiki/Moving_average#Exponential_moving_average) to smooth it out.

### Example
```cs
Motor motor = new Motor(0, 36, 250);
await motor.Initialize();
motor.Throttle = 50.0; // Set the throttle to 50%
var rpm = motor.RPM;   // Get the current RPM
```

### Constructor
```cs
Motor motor = new Motor(int motorPin, int encoderPin, int encoderPPR);
```

  * ```motorPin``` - The PWM channel on the HAT to which the motor is connected
  * ```encoderPin``` - The pin to which the optical encoder channel is connected
  * ```encoderPPR``` - The pulses per revolution value of the encoder used (found in the datasheet)

### Methods
| Method       | Description                                                                                              |
|--------------|----------------------------------------------------------------------------------------------------------|
| Initialize() | Initializes the motor. Must be called before any other operations are performed on the **Motor** object. |

### Properties
| Property | Type         | Access  | Description                                                                                                                          |
|----------|--------------|---------|--------------------------------------------------------------------------------------------------------------------------------------|
| Throttle | ```double``` | get/set | The throttle of the motor as a percentage between -100.0 and 100.0. -100.0 is full reverse, 0 is stopped, and 100.0 is full forward. |
| RPM      | ```double``` | get     | The current RPM of the motor.                                                                                                        |

## PidController
This project contains the PID logic for achieving closed loop control of the motor RPM and is documented [here](https://github.com/ms-iot/pid-controller).

## Microsoft.IoT.Lightning.Providers
This project is the WinRT layer that connects to the underlying Lightning (DMAP) layer and is documented [here](https://github.com/ms-iot/BusProviders/tree/develop/Microsoft.IoT.Lightning.Providers).