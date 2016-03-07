#pragma once

#include "Encoder.h"

using namespace Microsoft::IoT::Lightning::Providers;
using namespace Windows::Devices::Pwm;
using namespace Windows::Foundation;
using namespace concurrency;

namespace Microsoft
{
namespace Maker
{
	// By using a scope to measure the actual output frequency we found
	// that the internal clock on the PCA9685 generated a PWM frequency
	// that was 6.3% too fast. Setting the frequency to 94 yields a 
	// 99.7Hz PWM signal
	#define PWM_FREQUENCY 94
	#define PWM_ACTUAL_FREQUENCY 99.7
	#define THROTTLE_RANGE 0.0005		// 0.5 milliseconds
	#define THROTTLE_MIDPOINT	0.0015  // 1.5 milliseconds

	public ref class Motor sealed
	{
	private:
		int _motorPin;
		int _encoderPin;
		Encoder ^_encoder;
		PwmController ^_pwmController;
		PwmPin ^_motorPwm;
		double _throttle;
		void SetThrottle(double percent);

	public:
		Motor(int motorPin, int encoderPin, int encoderPPR);
		IAsyncAction^ Initialize();

		property double Throttle {
			double get() { return _throttle; }
			void set(double value)
			{
				SetThrottle(value);
			}
		}

		property int RPM {
			int get() { return _encoder->GetCurrentRPM(); }
		}

		property int DroppedInterruptCount {
			int get() { return _encoder->GetCurrentDropCount(); }
		}

	};
} // end Maker namespace
} // end Microsoft namespace
