using UnityEngine;

namespace Objects
{
    /// <summary>
    /// A script that just rotates the object forever
    /// </summary>
    public class RotatorMotor : MonoBehaviour
    {
        [SerializeField] private float _degreesPerSecond = 10;
        [SerializeField] private Vector3 m_axis = Vector3.forward;

        private void LateUpdate()
        {
            transform.Rotate(m_axis, _degreesPerSecond * Time.deltaTime);
        }
    }
}
