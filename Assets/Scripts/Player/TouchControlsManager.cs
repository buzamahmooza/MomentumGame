using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Player
{
    public class TouchControlsManager : MonoBehaviour
    {
        private Vector2 _touchOrigin = -Vector2.one;

        [SerializeField] [Range(0, 5)] private float _inputSmoother = 0.5f;

        //TODO: multiply the touchjoystickAmplitude by device pixel density
        /// <summary> how much far does the touch have to be to reach max speed? </summary>
        [SerializeField] [Range(0.1f, 500)] private float _touchJoystickAmplitude = 100f;

        [SerializeField] private GameObject _touchNob1, _touchNob2;
        [SerializeField] [Range(0f, 1f)] private float _deadZone = 0.1f;


        private void Update()
        {
//Check if we are running either in the Unity editor or in a standalone build.
#if UNITY_STANDALONE || UNITY_WEBPLAYER
       
//Check if we are running on iOS, Android, Windows Phone 8 or Unity iPhone
#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
//Check if Input has registered more than zero touches
            Vector2 dragOffset = Vector2.zero;
            if (Input.touchCount > 0)
            {
                //Store the first touch detected.
                Touch myTouch = Input.touches[0];

                switch (myTouch.phase)
                {
                    //Check if the phase of that touch equals Began
                    case TouchPhase.Began:
                        //If so, set touchOrigin to the position of that touch
                        _touchOrigin = myTouch.position;

                        // enable the nobs

                        _touchNob1.transform.position = _touchOrigin;
                        _touchNob2.transform.position = _touchOrigin;
                        break;
                    case TouchPhase.Moved:
                        _touchNob1.SetActive(true);
                        _touchNob2.SetActive(true);
                        break;
                    case TouchPhase.Canceled:
                    case TouchPhase.Ended:
                        //Set touchOrigin.x to -1 so that our default if statement will evaluate false and not repeat immediately.
                        _touchOrigin.x = -1;
                        dragOffset = Vector2.zero;
                        // disable touch nobs
                        _touchNob1.SetActive(false);
                        _touchNob2.SetActive(false);
                        break;
                    default:
                        if (_touchOrigin.x >= 0)
                        {
                            //Calculate the difference between the beginning and end of the touch.
                            var scaledOffset = (myTouch.position - _touchOrigin) / _touchJoystickAmplitude;
                            // clamp the magnitude to 1
                            dragOffset = Vector2.ClampMagnitude(scaledOffset, 1);
                            _touchNob2.transform.position = _touchOrigin + dragOffset * _touchJoystickAmplitude;
                        }
                        break;
                }
                
                if (dragOffset.magnitude <= _deadZone)
                    dragOffset = Vector2.zero;

                Debug.DrawLine(_touchOrigin, myTouch.position, Color.green);

                // set the CrossPlatformInputManager axis, but use lerp to make the transition smoothe
                CrossPlatformInputManager.SetAxis("Horizontal", dragOffset.x);
                CrossPlatformInputManager.SetAxis("Vertical", dragOffset.y);
            }

#endif //End of mobile platform dependendent compilation section started above with #elif
        }
    }
}