using UnityEngine;

namespace Objects
{
    /// <summary>
    /// Manages a pair of teleporters
    /// Note: Teleporters must be face opposite directions (one should be facing left and the other facing right)
    /// (i.e if you go in one facing right, you must come out the other facing right)
    /// </summary>
    [ExecuteInEditMode]
    public class PortalEditor : MonoBehaviour
    {
        /// <summary> portal </summary>
        [SerializeField] private Portal _portal1, _portal2;

        [SerializeField] private bool _flipPortals = false;
        [SerializeField] private bool _autoOrientate = true;
        [SerializeField] private bool _debug = false;

        /// <summary>
        /// indicates the current status of the portal locations (if there's an error or not)
        /// </summary>
        private Color m_statusColor = Color.green;


        void Start()
        {
            if (_portal1 == null || _portal2 == null)
            {
                _portal1 = transform.GetChild(0).GetComponent<Portal>();
                _portal2 = transform.GetChild(1).GetComponent<Portal>();
            }

            _portal1.OtherPortal = _portal2;
            _portal2.OtherPortal = _portal1;
        }

        void Update()
        {
            if (_flipPortals)
            {
                FlipBothPortals();
                _flipPortals = false;
                return;
            }

            // draw line between them to make it clear for the user
            Debug.DrawLine(_portal1.transform.position, _portal2.transform.position, m_statusColor);

            // draw a line to the spawn position for each portal
            Debug.DrawLine(
                new Vector2(_portal1.transform.position.x, _portal1.SpawnTransform.position.y),
                _portal1.SpawnTransform.position, Color.blue
            );
            Debug.DrawLine(
                new Vector2(_portal2.transform.position.x, _portal2.SpawnTransform.position.y),
                _portal2.SpawnTransform.position, Color.blue
            );

            if (IsPortalBlockedRight(_portal1.gameObject) && IsPortalBlockedRight(_portal2.gameObject) ||
                IsPortalBlockedLeft(_portal1.gameObject) && IsPortalBlockedLeft(_portal2.gameObject) ||
                IsPortalSpawnBlocked(_portal1) || IsPortalSpawnBlocked(_portal2))
            {
                m_statusColor = Color.red;
            }
            else
            {
                m_statusColor = Color.green;
            
                if (_autoOrientate)
                    if (IsPortalBlockedRight(_portal1.gameObject))
                    {
                        if(_debug)Debug.LogWarning(_portal1.name + " IsPortalBlockedRight");
                        FaceLeft(_portal1.transform);
                        FaceRight(_portal2.transform);
                    }
                    else if (IsPortalBlockedLeft(_portal2.gameObject))
                    {
                        if(_debug)Debug.LogWarning(_portal2.name + " IsPortalBlockedLeft ");
                        FaceLeft(_portal1.transform);
                        FaceRight(_portal2.transform);
                    }
                    else if (IsPortalBlockedRight(_portal2.gameObject))
                    {
                        if(_debug)Debug.LogWarning(_portal2.name + " IsPortalBlockedRight");
                        FaceLeft(_portal2.transform);
                        FaceRight(_portal1.transform);
                    }
                    else if (IsPortalBlockedLeft(_portal1.gameObject))
                    {
                        if(_debug)Debug.LogWarning(_portal1.name + " IsPortalBlockedLeft");
                        FaceLeft(_portal2.transform);
                        FaceRight(_portal1.transform);
                    }
            }
        }

        static void FaceRight(Transform transform)
        {
            FaceDirection(transform, true);
        }

        static void FaceLeft(Transform transform)
        {
            FaceDirection(transform, false);
        }

        private static void FaceDirection(Transform transform, bool right)
        {
            transform.localScale = new Vector3(
                right ? 1 : -1,
                transform.localScale.y,
                transform.localScale.z
            );
        }

        private void FlipBothPortals()
        {
            if (_portal1.transform.localScale.x > 0) // if portal1 is right
            {
                FaceLeft(_portal1.transform);
                FaceRight(_portal2.transform);
            }
            else
            {
                FaceLeft(_portal2.transform);
                FaceRight(_portal1.transform);
            }
        }

        private static bool IsPortalSpawnBlocked(Portal portal)
        {
            return Physics2D.CircleCast(portal.SpawnTransform.position, radius: 0.5f, direction: Vector2.up, distance: 0.5f,
                layerMask: LayerMask.GetMask("Floor"));
        }

        /// <summary>
        /// checks if something is blocking the object from the right
        /// </summary>
        /// <param name="portal"></param>
        /// <returns></returns>
        private RaycastHit2D IsPortalBlockedRight(GameObject portal)
        {
            return Physics2D.Raycast(
                portal.transform.position, portal.transform.right, 1,
                LayerMask.GetMask("Floor")
            );
        }

        private RaycastHit2D IsPortalBlockedLeft(GameObject portal)
        {
            return Physics2D.Raycast(
                portal.transform.position, -portal.transform.right, 1,
                LayerMask.GetMask("Floor")
            );
        }
    }
}