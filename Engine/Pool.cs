using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine {

	public class Pool<T> : IDisposable where T : class {

		private TaskFactory _taskFactory = new TaskFactory();
		private Func<T> _constructor;
		private Action<T> _cleaner;
		private Action<T> _disposer;
		private ConcurrentStack<T> _pool;
		private int _maxSize;
		private int _minSize;

		public Pool(Func<T> constructor, int minSize = 8, int size = 16, int maxSize = 32, Action<T> cleanup = null, Action<T> disposer = null) {
			System.Diagnostics.Debug.Assert(constructor != null, "Null constructor passed to Pool");
			if (minSize < 2)
				minSize = 2;

			if (size < minSize)
				size = minSize;

			if (maxSize < minSize) {
				maxSize = minSize + 1;
			}

			_constructor = constructor;
			_maxSize = maxSize;
			_minSize = minSize;
			_pool = new ConcurrentStack<T>();

			T[] toPool = new T[size];

			for (int i = 0;  i < size; i++)
				toPool[i] = _constructor();

			_pool.PushRange(toPool);

			if (cleanup != null) {
				_cleaner = cleanup;
			} else {
				_cleaner = (obj) => { };
			}

			if (disposer != null)
				_disposer = disposer;
			else
				_disposer = (obj) => { };
		}

		public T Get() {
			if (_pool.Count < _minSize)
				_taskFactory.StartNew(_refillPool);

			T obj;
			if (_pool.TryPop(out obj))
				return obj;
			else
				return _constructor();
		}

		/// <summary>
		/// Returns the item to the pool and returns null to streamline
		/// removal of live references.
		/// </summary>
		/// <param name="item">The object to return</param>
		/// <returns>null</returns>
		public T Return(T item) {
			_cleaner(item);
			_pool.Push(item);

			if (_pool.Count >= _maxSize)
				_taskFactory.StartNew(_trimExcess);

			return null;
		}

		public void Dispose() {
			foreach (var item in _pool)
				_disposer(item);

			if (typeof(IDisposable).IsAssignableFrom(typeof(T))) {
				foreach (var item in _pool)
					(item as IDisposable).Dispose();
			}
		}

		private void _refillPool() {
			T[] objs = new T[_maxSize / 3];
			for (int i = 0, j = objs.Length; i < j; i++)
				objs[i] = _constructor();
			_pool.PushRange(objs);
		}

		private void _trimExcess() {
			if (_pool.Count <= _maxSize) return;
			T[] trimmed = new T[_pool.Count - _maxSize];
			int count = _pool.TryPopRange(trimmed, 0, trimmed.Length);
			for (int i = 0; i < count; i++) {
				_disposer(trimmed[i]);
			}
		}
	}
}
