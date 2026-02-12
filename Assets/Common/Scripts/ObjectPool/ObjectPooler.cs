using System.Collections.Generic;
using Common.Scripts.SceneSingleton;
using UnityEngine;

namespace Common.Scripts.ObjectPool
{
    struct Pair
    {
        public Transform transform;
        public List<GameObject> objects;

        public Pair(Transform transform, List<GameObject> objects)
        {
            this.transform = transform;
            this.objects = objects;
        }
    }
    
    public class ObjectPooler : SceneSingleton<ObjectPooler>
    {
        private Dictionary<string, Pair> poolingObjects = new();
        
        [SerializeField] private Transform poolParent;
        
        public GameObject Spawn(GameObject prefab, Transform parent, Vector2? position = null)
        {
            GameObject target = SpawnFromPool(prefab, parent);

            if (position.HasValue)
            {
                target.transform.position = position.Value;
            }

            target.SetActive(true);

            return target;
        }

        public GameObject SpawnUI(GameObject prefab, RectTransform parent, Vector2 anchoredPosition)
        {
            GameObject target = SpawnFromPool(prefab, parent);

            var rt = target.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPosition;

            target.SetActive(true);

            return target;
        }

        private GameObject SpawnFromPool(GameObject prefab, Transform parent)
        {
            string poolId = prefab.name + prefab.GetInstanceID();

            bool isExists = poolingObjects.TryGetValue(poolId, out var pool);

            if (!isExists)
            {
                GameObject newPool = new GameObject(poolId);

                newPool.transform.position = poolParent.position;

                pool = new Pair(newPool.transform, new List<GameObject>());
                newPool.transform.SetParent(poolParent);

                poolingObjects.Add(poolId, pool);
            }

            GameObject target = pool.objects.Find(poolable => !poolable.activeSelf);

            if (target == null)
            {
                target = Instantiate(prefab, pool.transform, true);

                Poolable poolable = target.GetComponent<Poolable>();
                poolable.SetPoolId(poolId);

                pool.objects.Add(target);
            }

            target.transform.SetParent(parent);

            return target;
        }

        public void DeSpawn(Poolable poolable)
        {
            if (!poolingObjects[poolable.poolId].objects.Contains(poolable.gameObject))
            {
                Debug.LogError("Poolable 이 해당 부모의 자식이 아닙니다.");
                return;
            }

            poolable.gameObject.SetActive(false);
            poolable.transform.SetParent(poolingObjects[poolable.poolId].transform);
            poolable.transform.position = poolingObjects[poolable.poolId].transform.position;
        }
    }   
}
