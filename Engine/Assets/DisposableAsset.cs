using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Assets {
	public abstract class DisposableAsset<T> : IDisposable where T : IDisposable {
		private static readonly ConcurrentDictionary<T, int> _referenceCount = new ConcurrentDictionary<T, int>();
		public readonly T Resource;
		public bool IsDisposed { get; private set; }

		internal DisposableAsset(T resource) {
			System.Diagnostics.Debug.Assert(resource != null, "Bitmap resource should not be null");
			Resource = resource;
			_referenceCount.AddOrUpdate(resource, 1, (key, oldValue) => oldValue + 1);
			IsDisposed = false;
		}

		~DisposableAsset() {
			Dispose(false);
		}

		/// <summary>
		/// Releases resources held by the control
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases resources held by the control
		/// </summary>
		/// <param name="disposing">Whether managed resources should be released as well as native resources</param>
		protected virtual void Dispose(bool disposing) {
			if (IsDisposed)
				return;

			_referenceCount.AddOrUpdate(Resource, 0, (key, oldValue) => oldValue - 1);
			int referencesRemaining = 0;
			_referenceCount.TryGetValue(Resource, out referencesRemaining);
			if (referencesRemaining < 1) {
				Resource.Dispose();
				_referenceCount.TryRemove(Resource, out referencesRemaining);
			}
			IsDisposed = true;
		}
	}
}
