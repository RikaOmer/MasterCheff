using UnityEngine;
using System.Collections.Generic;

namespace MasterCheff.Utils
{
    /// <summary>
    /// Generic Object Pool for efficient object reuse
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int initialSize = 10;
            public bool expandable = true;
            public int maxSize = 100;
        }

        [SerializeField] private List<Pool> _pools;
        [SerializeField] private bool _organizeHierarchy = true;

        private Dictionary<string, Queue<GameObject>> _poolDictionary;
        private Dictionary<string, Pool> _poolSettings;
        private Dictionary<string, Transform> _poolContainers;
        private Dictionary<string, int> _activeCount;

        private static ObjectPool _instance;
        public static ObjectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Unity 2022.3+ uses FindFirstObjectByType instead of deprecated FindObjectOfType
                    _instance = FindFirstObjectByType<ObjectPool>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[ObjectPool]");
                        _instance = go.AddComponent<ObjectPool>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Initialize();
        }

        private void Initialize()
        {
            _poolDictionary = new Dictionary<string, Queue<GameObject>>();
            _poolSettings = new Dictionary<string, Pool>();
            _poolContainers = new Dictionary<string, Transform>();
            _activeCount = new Dictionary<string, int>();

            if (_pools == null) return;

            foreach (Pool pool in _pools)
            {
                CreatePool(pool);
            }
        }

        private void CreatePool(Pool pool)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            // Create container for organization
            Transform container = null;
            if (_organizeHierarchy)
            {
                GameObject containerObj = new GameObject($"Pool_{pool.tag}");
                containerObj.transform.SetParent(transform);
                container = containerObj.transform;
                _poolContainers[pool.tag] = container;
            }

            // Pre-instantiate objects
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = CreateNewObject(pool.prefab, container);
                objectPool.Enqueue(obj);
            }

            _poolDictionary[pool.tag] = objectPool;
            _poolSettings[pool.tag] = pool;
            _activeCount[pool.tag] = 0;
        }

        private GameObject CreateNewObject(GameObject prefab, Transform parent)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.SetActive(false);
            
            // Add pooled object component for tracking
            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = obj.AddComponent<PooledObject>();
            }

            return obj;
        }

        #region Public Methods

        /// <summary>
        /// Register a new pool at runtime
        /// </summary>
        public void RegisterPool(string tag, GameObject prefab, int initialSize = 10, bool expandable = true)
        {
            if (_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPool] Pool already exists: {tag}");
                return;
            }

            Pool pool = new Pool
            {
                tag = tag,
                prefab = prefab,
                initialSize = initialSize,
                expandable = expandable
            };

            CreatePool(pool);
        }

        /// <summary>
        /// Get an object from the pool
        /// </summary>
        public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPool] Pool doesn't exist: {tag}");
                return null;
            }

            Pool poolSettings = _poolSettings[tag];
            Queue<GameObject> pool = _poolDictionary[tag];

            GameObject obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else if (poolSettings.expandable && _activeCount[tag] < poolSettings.maxSize)
            {
                Transform container = _poolContainers.ContainsKey(tag) ? _poolContainers[tag] : transform;
                obj = CreateNewObject(poolSettings.prefab, container);
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Pool exhausted: {tag}");
                return null;
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            _activeCount[tag]++;

            // Notify pooled object
            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj != null)
            {
                pooledObj.OnSpawn();
            }

            return obj;
        }

        /// <summary>
        /// Get an object from the pool with parent
        /// </summary>
        public GameObject Spawn(string tag, Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject obj = Spawn(tag, position, rotation);
            if (obj != null)
            {
                obj.transform.SetParent(parent);
            }
            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Despawn(string tag, GameObject obj)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPool] Pool doesn't exist: {tag}");
                Destroy(obj);
                return;
            }

            // Notify pooled object
            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj != null)
            {
                pooledObj.OnDespawn();
            }

            obj.SetActive(false);

            // Return to original container
            if (_poolContainers.ContainsKey(tag))
            {
                obj.transform.SetParent(_poolContainers[tag]);
            }

            _poolDictionary[tag].Enqueue(obj);
            _activeCount[tag]--;
        }

        /// <summary>
        /// Despawn after delay
        /// </summary>
        public void DespawnDelayed(string tag, GameObject obj, float delay)
        {
            StartCoroutine(DespawnAfterDelay(tag, obj, delay));
        }

        private System.Collections.IEnumerator DespawnAfterDelay(string tag, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null && obj.activeInHierarchy)
            {
                Despawn(tag, obj);
            }
        }

        /// <summary>
        /// Clear all objects from a specific pool
        /// </summary>
        public void ClearPool(string tag)
        {
            if (!_poolDictionary.ContainsKey(tag)) return;

            Queue<GameObject> pool = _poolDictionary[tag];
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            _activeCount[tag] = 0;
        }

        /// <summary>
        /// Get the number of available objects in a pool
        /// </summary>
        public int GetAvailableCount(string tag)
        {
            return _poolDictionary.ContainsKey(tag) ? _poolDictionary[tag].Count : 0;
        }

        /// <summary>
        /// Get the number of active objects from a pool
        /// </summary>
        public int GetActiveCount(string tag)
        {
            return _activeCount.ContainsKey(tag) ? _activeCount[tag] : 0;
        }

        #endregion
    }

    /// <summary>
    /// Component attached to pooled objects
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public string PoolTag { get; set; }

        public virtual void OnSpawn()
        {
            // Override in derived classes
        }

        public virtual void OnDespawn()
        {
            // Override in derived classes
        }

        /// <summary>
        /// Return this object to its pool
        /// </summary>
        public void ReturnToPool()
        {
            if (!string.IsNullOrEmpty(PoolTag))
            {
                ObjectPool.Instance.Despawn(PoolTag, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}

