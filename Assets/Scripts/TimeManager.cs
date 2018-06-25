using UnityEngine;

public class TimeManager : MonoBehaviour
{

    public float slowDownFactor = 0.05f;
    public float slowDownLength = 2.0f;


    public void DoSlowMotion() {
        Time.timeScale = slowDownFactor;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    // Update is called once per frame
    private void Update() {
        Time.timeScale = Mathf.Clamp(Time.timeScale + 1 / slowDownLength * Time.unscaledDeltaTime, 0f, 1f);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        //Mathf.Lerp(Time.timeScale, 1, Time.deltaTime);
    }
}
