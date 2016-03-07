#include "pch.h"
#include <pplawait.h>
#include "Motor.h"

using namespace Microsoft::Maker;
using namespace Platform;
using namespace Microsoft::IoT::Lightning::Providers;
using namespace Windows::Devices::Pwm;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Concurrency;

Microsoft::Maker::Motor::Motor(int motorPin, int encoderPin, int encoderPPR) : 
	_motorPin(motorPin), 
	_encoderPin(encoderPin), 
	_throttle(0), 
	_encoder(nullptr), 
	_pwmController(nullptr), 
	_motorPwm(nullptr)
{
	_encoder = ref new Encoder(encoderPin, encoderPPR);
}

IAsyncAction^ Microsoft::Maker::Motor::Initialize()
{
	return create_async([this] {
		create_task(PwmController::GetControllersAsync(LightningPwmProvider::GetPwmProvider())).then([this](IVectorView<PwmController^> ^providers) {
			_pwmController = providers->GetAt(0);
			_motorPwm = _pwmController->OpenPin(_motorPin);
			_pwmController->SetDesiredFrequency(PWM_FREQUENCY);
			SetThrottle(0);
			_motorPwm->Start();
		}).get();
	});
}

void Microsoft::Maker::Motor::SetThrottle(double percent)
{
	if (nullptr != _motorPwm)
	{
		_throttle = percent;
		if (_throttle > 100.0) _throttle = 100.0;
		if (_throttle < -100.0) _throttle = -100.0;

		// For our motor the speed is determined by a pulse width in the range of 1-2ms.
		// 1ms is full reverse, 1.5ms is stopped, and 2ms is full forward.
		// Map the throttle percentage into the motor range and adjust it based on
		// the actual PWM frequency
		double actualDutyCyclePercentage = THROTTLE_MIDPOINT + (THROTTLE_RANGE * (_throttle / 100.0));
		actualDutyCyclePercentage *= PWM_ACTUAL_FREQUENCY;
		_motorPwm->SetActiveDutyCyclePercentage(actualDutyCyclePercentage);
	}
}
