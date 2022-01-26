using UnityEngine;

namespace Backgammon.Helpers.ObjectPool
{
    // Added to freshly instantiated objects, to link to the correct pool on despawn.
    public class PoolMember : MonoBehaviour
    {
        public ObjectPool.Pool myPool;
    }
}
