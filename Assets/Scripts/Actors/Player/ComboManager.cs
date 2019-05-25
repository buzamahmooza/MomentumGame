using UnityEngine;
using UnityEngine.Serialization;

namespace Actors.Player
{
    public class ComboManager : MonoBehaviour
    {
        [SerializeField] public UnityEngine.UI.Text ComboText;

        public void DisplayCombo(int comboCount) {
        
#if !UNITY_EDITOR
        comboText.enabled = false;
#endif
            ComboText.text = comboCount == 0 ? "No combo..." : string.Format("{0} hit combo!", comboCount);
        }
    }
}
