using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine {
	/// <summary>
	/// Maintains an internal timer and on calls to update, returns true if the
	/// configured interval has elapsed, simplifying timed operations.
	/// </summary>
	public class IntervalTimer {
		private GameTimer _timer = new GameTimer();
		private readonly long _interval;
		private long _acc = 0L;

		/// <summary>
		/// Initializes a new instance of the IntervalTimer class
		/// </summary>
		/// <param name="milliseconds">A float representing the length of the timed interval in milliseconds</param>
		public IntervalTimer(float milliseconds) : this((long)milliseconds * 10000L) {
		}

		/// <summary>
		/// Initializes a new instance of the IntervalTimer class
		/// </summary>
		/// <param name="milliseconds">A long representing the length of the timed interval in ticks</param>
		public IntervalTimer(long ticks) {
			_interval = ticks;
		}

		public void Start() {
			_timer.Start();
			_acc = 0L;
		}

		public void Stop() {
			_timer.Stop();
			_acc = 0L;
		}

		public void Reset() {
			_timer.Reset();
			_acc = 0L;
		}

		public bool Update() {
			if ((_acc += _timer.UpdateTicks()) > _interval) {
				_acc -= _interval;
				return true;
			}
			return false;
		}
	}
}
