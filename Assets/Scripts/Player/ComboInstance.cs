using UnityEngine;

public class ComboInstance
{
    /// indicates if the combo has already ended
    public bool HasEnded { get; private set; }
    /// <summary>
    /// The combo count of the current instance (each hit in the combo increments the count by one).
    /// </summary>
    public int Count { get; private set; }
    /// <summary> the maximum time between each consecutive hit without dropping the combo </summary>
    [SerializeField] private float maxTimeBetweenCombos = 2f;
    private System.Timers.Timer _timer;

    public ComboInstance() {
        HasEnded = false;
        SetTimer();
    }

    //TODO: this timer doesn't get affected by timescale, we need it to affect it, otherwise the slomotion will still count as normal time
    /// <summary>
    /// Sets the _timer of the combo,
    /// when the timer goes off, the combo will have ended.
    /// The combo is reset on IncrementCount()
    /// </summary>
    private void SetTimer() {
        if (_timer != null) _timer.Dispose();
        // create a timer and set its interval and enable it
        _timer = new System.Timers.Timer {
            Interval = maxTimeBetweenCombos * 1000,
            Enabled = true
        };

        // subscribe to the Elapsed event with an anonymous delegate function
        _timer.Elapsed += delegate {
            if (!HasEnded) {
                HasEnded = true;
                Debug.Log("ComboInstance ended cuz no attacks have been registered for longer than " + maxTimeBetweenCombos +
                          " seconds.");
            }
        };
    }

    public void IncrementCount() {
        if (HasEnded) { Debug.LogWarning("ComboInstance has already ended, you can't change it after that."); return; }
        Count++;
        SetTimer();
    }
}