using UnityEngine;
using System.Collections.Generic;

namespace Backgammon.Helpers.ObjectPool
{
    ///   No setup required.
    ///   Get new object - ObjectPool.Spawn()
    ///   Get rid of an object ObjectPool.Despawn()
    ///
    ///   If desired, preload the pool ObjectPool.Preload(somePrefab, 20);
    public static class ObjectPool
    {
        private const int defaultPoolSize = 30;
        private static Dictionary<MonoBehaviour, Pool> pools;
        private static Transform poolsContainer = null;

        // A pool for a particular prefab.
        public class Pool
        {
            private Stack<MonoBehaviour> inactiveObjects;
            private MonoBehaviour prefab;
            private int nextNameId = 1;
            private Transform poolParent;

            public Pool(MonoBehaviour prefab, int initialQty)
            {
                this.prefab = prefab;
                inactiveObjects = new Stack<MonoBehaviour>(initialQty);
                poolParent = new GameObject(prefab.name + " pool").transform;
                poolParent.parent = poolsContainer;
            }

            public T Spawn<T>(Vector3 atPosition, Quaternion withRotation, Transform customParent = null, bool uiElement = false) where T : MonoBehaviour
            {
                MonoBehaviour clone = null;

                if (inactiveObjects.Count > 0)
                {
                    clone = inactiveObjects.Pop();
                }

                if (clone == null)
                {
                    clone = Create();
                }

                clone.transform.position = atPosition;
                clone.transform.rotation = withRotation;
                if (uiElement)
                {
                    clone.GetComponent<RectTransform>().SetParent(customParent ?? poolParent, false);
                }
                else
                {
                    clone.transform.parent = customParent ?? poolParent;
                }
                clone.gameObject.SetActive(true);
                return clone as T;
            }

            private MonoBehaviour Create()
            {
                MonoBehaviour clone = Object.Instantiate(prefab);
                clone.name = prefab.name + " (" + (nextNameId++) + ")";

                // Add a PoolMember component to know what pool instance belongs to.
                clone.gameObject.AddComponent<PoolMember>().myPool = this;
                return clone;
            }

            public void Despawn(MonoBehaviour clone, bool isUIElement = false)
            {
                clone.gameObject.SetActive(false);
                if (isUIElement)
                {
                    clone.transform.SetParent(poolParent, false);
                }
                else
                {
                    clone.transform.parent = poolParent;
                }
                inactiveObjects.Push(clone);
            }
        }

        private static void Init<T>(T prefab = null, int qty = defaultPoolSize) where T : MonoBehaviour
        {
            if (pools == null)
            {
                pools = new Dictionary<MonoBehaviour, Pool>();
                poolsContainer = new GameObject("PoolsContainer").transform;
            }
            if (prefab != null && pools.ContainsKey(prefab) == false)
            {
                pools[prefab] = new Pool(prefab, qty);
            }
        }

        // Preload instances, if quick jump in quantity is expected
        public static void Preload<T>(T prefab, int amount = 1) where T : MonoBehaviour
        {
            Init(prefab, amount);

            T[] objects = new T[amount];
            for (int i = 0; i < amount; i++)
            {
                objects[i] = Spawn(prefab, Vector3.zero, Quaternion.identity);
            }

            for (int i = 0; i < amount; i++)
            {
                Despawn<T>(objects[i]);
            }
        }

        public static T Spawn<T>(T prefab, Vector3 pos, Quaternion rot, Transform customParent = null, bool isUIElement = false) where T : MonoBehaviour
        {
            Init(prefab);

            return pools[prefab].Spawn<T>(pos, rot, customParent, isUIElement);
        }

        // Despawn the specified object back into its pool.
        public static void Despawn<T>(T clone, bool isUIElement = false) where T : MonoBehaviour
        {
            PoolMember pm = clone.GetComponent<PoolMember>();
            if (pm == null)
            {
                Debug.Log("Object '" + clone.name + "' wasn't spawned from a pool. Destroying it instead.");
                GameObject.Destroy(clone);
            }
            else
            {
                pm.myPool.Despawn(clone, isUIElement);
            }
        }
    }
}
