using System;

namespace Engine {
	public class GameTimer {
		private long _lastTicks = 0L;
		private long _totalElapsedTicks;
		private bool _running = false;

		public TimeSpan Elapsed { 
			get {
				this.Update();
				return new TimeSpan(_totalElapsedTicks); 
			}
		}

		public long ElapsedTicks {
			get {
				this.Update();
				return _totalElapsedTicks;
			}
		}

		public void Start() {
			_lastTicks = DateTime.Now.Ticks;
			_totalElapsedTicks = 0L;
			_running = true;
		}

		public void Stop() {
			_running = false;
		}

		/// <summary>
		/// Returns the number of milliseconds that have elapsed
		/// since the last call to Update() or UpdateTicks()
		/// </summary>
		/// <returns>double</returns>
		public double Update() {
			long elapsedTicks = UpdateTicks();
			if (elapsedTicks == 0L)
				return elapsedTicks;

			return elapsedTicks / 10000.0;
		}

		/// <summary>
		/// Returns the number of ticks that have elapsed
		/// since the last call to Update() or UpdateTicks()
		/// </summary>
		/// <returns>long</returns>
		public long UpdateTicks() {
			if (!_running) return 0L;

			long now = DateTime.Now.Ticks;
			long elapsed = now - _lastTicks;
			_totalElapsedTicks += elapsed;
			_lastTicks = now;
			return elapsed;
		}

		public void Reset() {
			_lastTicks = DateTime.Now.Ticks;
			_totalElapsedTicks = 0L;
		}
	}
}
