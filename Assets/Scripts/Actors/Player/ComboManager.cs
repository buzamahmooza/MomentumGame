using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboManager : MonoBehaviour
{
    [SerializeField] public UnityEngine.UI.Text comboText;

    public void DisplayCombo(int comboCount) {
        
#if !UNITY_EDITOR
        comboText.enabled = false;
#endif
        comboText.text = comboCount == 0 ? "No combo..." : string.Format("{0} hit combo!", comboCount);
    }
}
