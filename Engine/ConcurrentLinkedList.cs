using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace System.Collections.Concurrent {
	public class ConcurrentLinkedList<T> {

		#region Fields/Properties

		private LinkedList<T> _list = new LinkedList<T>();
		private SpinLock _lock = new SpinLock();
		public LinkedListNode<T> First { get { return _list.First; } }
		public LinkedListNode<T> Last { get { return _list.Last; } }
		public int Count { get { return _list.Count; } }

		#endregion

		#region LinkedList wrapper

		public bool Contains(T item) {
			return _list.Contains(item);
		}

		public bool TryAddFirst(T item) {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.AddFirst(item);
			} finally {
				_lock.Exit(true);
			}
			return true;
		}

		public bool TryAddFirst(LinkedListNode<T> item) {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.AddFirst(item);
			} finally {
				_lock.Exit(true);
			}
			return true;
		}

		public bool TryAddLast(T item) {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.AddLast(item);
			} finally {
				_lock.Exit(true);
			}
			return true;
		}
		public bool TryAddLast(LinkedListNode<T> item) {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.AddLast(item);
			} finally {
				_lock.Exit(true);
			}
			return true;
		}

		public bool TryAddBefore(LinkedListNode<T> item, T newItem) {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.AddBefore(item, newItem);
			} finally {
				_lock.Exit(true);
			}
			return true;
		}

		public bool TryAddBefore(LinkedListNode<T> item, LinkedListNode<T> newItem) {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.AddBefore(item, newItem);
			} finally {
				_lock.Exit(true);
			}
			return true;
		}

		public bool TryAddAfter(LinkedListNode<T> item, T newItem) {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.AddAfter(item, newItem);
			} finally {
				_lock.Exit(true);
			}
			return true;
		}

		public bool TryAddAfter(LinkedListNode<T> item, LinkedListNode<T> newItem) {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.AddAfter(item, newItem);
			} finally {
				_lock.Exit(true);
			}
			return true;
		}

		public bool TryRemove(T item) {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.Remove(item);
			} finally {
				_lock.Exit(true);
			}
			return true;
		}

		public bool TryRemove(LinkedListNode<T> item) {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.Remove(item);
			} finally {
				_lock.Exit(true);
			}
			return true;
		}

		public bool TryRemoveLast() {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.RemoveLast();
			} finally {
				_lock.Exit(true);
			}
			return true;
		}

		public bool TryRemoveFirst() {
			bool lockTaken = false;
			_lock.Enter(ref lockTaken);
			if (!lockTaken)
				return false;
			try {
				_list.RemoveFirst();
			} finally {
				_lock.Exit(true);
			}
			return true;
		}
		#endregion
	}
}
