using System;
using UnityEngine;

namespace UnityStandardAssets.CrossPlatformInput
{
    public class ButtonHandler : MonoBehaviour
    {
        public string Name;

        void OnEnable()
        {
        }

        public void SetDownState()
        {
            print(Name + " SetDownState");
            CrossPlatformInputManager.SetButtonDown(Name);
        }


        public void SetUpState()
        {
            print(Name + " SetUpState");
            CrossPlatformInputManager.SetButtonUp(Name);
        }


        public void SetAxisPositiveState()
        {
            print(Name + " SetAxisPositiveState");
            CrossPlatformInputManager.SetAxisPositive(Name);
        }


        public void SetAxisNeutralState()
        {
            print(Name + " SetAxisNeutralState");
            CrossPlatformInputManager.SetAxisZero(Name);
        }


        public void SetAxisNegativeState()
        {
            print(Name + " SetAxisNegativeState");
            CrossPlatformInputManager.SetAxisNegative(Name);
        }

        public void Update()
        {
        }
    }
}