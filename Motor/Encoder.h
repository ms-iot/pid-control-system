#pragma once

#include "Wire.h"
#include "arduino.h"
#include <mutex>

#define DEGREES_PER_REVOLUTION	360.00

namespace Microsoft {
namespace Maker {
	public ref class Encoder sealed
	{
	private:
		uint64_t _lastEventTime;
		uint32_t _currentRPM;
		uint32_t _dropCount;
		double _degreesPerPulse;
		double _accumulator;
		LARGE_INTEGER _hiResolutionClockFrequency;
		bool _isFirstInterrupt;
		int _pin;

		static void InterruptCallback(Encoder ^m, PDMAP_WAIT_INTERRUPT_NOTIFY_BUFFER pInfo);

	public:
		Encoder(int pin, int pulsesPerRevolution);
		virtual ~Encoder();
		uint32_t GetCurrentRPM(void);
		uint32_t GetCurrentDropCount(void);
	};

} // end Maker namespace
} // end Microsoft namespace

