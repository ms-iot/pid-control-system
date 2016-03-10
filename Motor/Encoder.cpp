#include "pch.h"
#include "Encoder.h"

using namespace Microsoft::Maker;
using namespace Platform;
using namespace std::placeholders;

std::function<void(PDMAP_WAIT_INTERRUPT_NOTIFY_BUFFER)> f;

Microsoft::Maker::Encoder::Encoder(int pin, int pulsesPerRevolution) : _currentRPM(0), _dropCount(0), _isFirstInterrupt(true), _pin(pin)
{
	_degreesPerPulse = DEGREES_PER_REVOLUTION / pulsesPerRevolution;

	// The high resolution clock frequency is fixed at system boot so we only
	// need to query this value once
	QueryPerformanceFrequency(&_hiResolutionClockFrequency);

	// Set up the interrupt handler
	pinMode(pin, INPUT);
	f = std::bind(Encoder::InterruptCallback, this, _1);
	attachInterruptEx(pin, f, RISING);
}

Microsoft::Maker::Encoder::~Encoder()
{
	detachInterrupt(_pin);
}

uint32_t Microsoft::Maker::Encoder::GetCurrentRPM(void)
{
	return _currentRPM;
}

uint32_t Microsoft::Maker::Encoder::GetCurrentDropCount(void)
{
	return _dropCount;
}

void Encoder::InterruptCallback(Encoder ^m, PDMAP_WAIT_INTERRUPT_NOTIFY_BUFFER pInfo)
{
	if (m->_isFirstInterrupt) {
		m->_lastEventTime = pInfo->EventTime;
		m->_isFirstInterrupt = false;
	}
	else
	{
		uint64_t currentEventTime = pInfo->EventTime;

		// How far has the shaft traveled since our last interrupt
		double degreesSinceLastInterrupt = m->_degreesPerPulse * (1 + pInfo->DropCount);

		// Convert the elapsed time to microseconds. For more info on the high-resolution clock see
		// https://msdn.microsoft.com/en-us/library/windows/desktop/dn553408(v=vs.85).aspx
		uint64_t elapsedTime = currentEventTime - m->_lastEventTime;
		elapsedTime *= 1000000;
		elapsedTime /= m->_hiResolutionClockFrequency.QuadPart; // Get elapsed time in microseconds

		// Calculate the RPM
		// degreesSinceLastInterrupt      1 revolution       60000000 us
		// -------------------------  x  --------------  x  -------------  =  RPM Value
		//     elapsedTime in us           360 degrees          1 min
		uint32_t  newValue = static_cast<uint32_t>((degreesSinceLastInterrupt * 60000000.00) / (DEGREES_PER_REVOLUTION * elapsedTime));

		// Smooth the RPM output using an exponential moving average. The 
		// following uses an alpha value of 0.05;
		// https://en.wikipedia.org/wiki/Moving_average#Exponential_moving_average
		m->_currentRPM = static_cast<uint32_t>(EMA_ALPHA*newValue + (1.0 - EMA_ALPHA)*m->_currentRPM);

		// Keep track of the drop count so we know how well we're keeping up
		// with the interrupts
		m->_dropCount = pInfo->DropCount;
		m->_lastEventTime = currentEventTime;
	}
}
