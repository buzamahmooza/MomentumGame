using UnityEngine;

namespace Actors
{
    public class Targeting : MonoBehaviour
    {
        [SerializeField] public Transform Target;

        public virtual Vector2 AimDirection => Target ?
            (Vector2)(Target.transform.position - transform.position) :
            Vector2.right;
    }
}