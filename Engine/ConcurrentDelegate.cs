using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Engine {
	public class ConcurrentDelegate {
		private Action _action = () => { };
		private SpinLock _lock = new SpinLock();

		public void AddAction(Action newAction) {
			Debug.Assert(newAction != null, "Can not add a null action to a concurrent delegate");
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				throw new SynchronizationLockException("Could not acquire lock in concurrent delegate.");
			try {
				_action += newAction;
			} finally {
				_lock.Exit(true);
			}
		}

		public void RemoveAction(Action toRemove) {
			Debug.Assert(toRemove != null, "Can not remove a null action from a concurrent delegate");
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				throw new SynchronizationLockException("Could not acquire lock in concurrent delegate.");
			try {
				_action -= toRemove;
			} finally {
				_lock.Exit(true);
			}
		}

		public void Execute() {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				Console.Error.WriteLine("Could not execute concurrent action.  Failed to acquire lock.");
			
			try {
				_action();
			} finally {
				_lock.Exit(false);
			}
		}
	}

	public class ConcurrentDelegate<T> {
		private Action<T> _action = (p1) => { };
		private SpinLock _lock = new SpinLock();

		public void AddAction(Action<T> newAction) {
			Debug.Assert(newAction != null, "Can not add a null action to a concurrent delegate");
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				throw new SynchronizationLockException("Could not acquire lock in concurrent delegate.");
			try {
				_action += newAction;
			} finally {
				_lock.Exit(true);
			}
		}

		public void RemoveAction(Action<T> toRemove) {
			Debug.Assert(toRemove != null, "Can not remove a null action from a concurrent delegate");
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				throw new SynchronizationLockException("Could not acquire lock in concurrent delegate.");
			try {
				_action -= toRemove;
			} finally {
				_lock.Exit(true);
			}
		}

		public void Execute(T parameter) {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				Console.Error.WriteLine("Could not execute concurrent action.  Failed to acquire lock.");

			try {
				_action(parameter);
			} finally {
				_lock.Exit(false);
			}
		}
	}
}
