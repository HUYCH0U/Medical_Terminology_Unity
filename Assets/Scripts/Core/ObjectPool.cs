using UnityEngine;
using System.Collections.Generic;

namespace MedicalTerminology.Core
{
    /// <summary>
    /// Generic object pool for performance optimization.
    /// Eliminates GC allocation and instantiation overhead.
    /// </summary>
    /// <typeparam name="T">Component type to pool</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly int _initialSize;
        private readonly int _maxSize;
        
        private readonly Stack<T> _available = new Stack<T>();
        private readonly HashSet<T> _inUse = new HashSet<T>();

        public ObjectPool(T prefab, int initialSize = 10, int maxSize = 50, Transform parent = null)
        {
            _prefab = prefab;
            _initialSize = initialSize;
            _maxSize = maxSize;
            _parent = parent;
            
            WarmUp();
        }

        /// <summary>
        /// Pre-instantiate objects to avoid runtime allocation spikes.
        /// </summary>
        private void WarmUp()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var obj = CreateNew();
                _available.Push(obj);
            }
        }

        /// <summary>
        /// Create a new object instance.
        /// </summary>
        private T CreateNew()
        {
            var obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            return obj;
        }

        /// <summary>
        /// Get an object from the pool. Creates new if pool exhausted.
        /// </summary>
        public T Get()
        {
            T obj = _available.Count > 0 ? _available.Pop() : CreateNew();
            
            obj.gameObject.SetActive(true);
            _inUse.Add(obj);
            
            return obj;
        }

        /// <summary>
        /// Return object to pool for reuse.
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[ObjectPool] Attempting to return null object");
                return;
            }
            
            if (!_inUse.Remove(obj))
            {
                Debug.LogWarning($"[ObjectPool] Object {obj.name} was not checked out from this pool");
                return;
            }
            
            obj.gameObject.SetActive(false);
            
            // Prevent pool bloat
            if (_available.Count < _maxSize)
            {
                _available.Push(obj);
            }
            else
            {
                Object.Destroy(obj.gameObject);
            }
        }

        /// <summary>
        /// Return all active objects to pool.
        /// </summary>
        public void ReturnAll()
        {
            var objectsInUse = new List<T>(_inUse);
            foreach (var obj in objectsInUse)
            {
                Return(obj);
            }
        }

        /// <summary>
        /// Clear and destroy all pooled objects.
        /// </summary>
        public void Clear()
        {
            ReturnAll();
            
            while (_available.Count > 0)
            {
                var obj = _available.Pop();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }
        }

        /// <summary>
        /// Get pool statistics for debugging.
        /// </summary>
        public (int available, int inUse, int total) GetStats()
        {
            return (_available.Count, _inUse.Count, _available.Count + _inUse.Count);
        }
    }
}
