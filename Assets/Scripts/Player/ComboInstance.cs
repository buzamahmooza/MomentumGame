using System.Timers;
using UnityEngine;

/// <summary>
/// NOTE: Instances of ComboInstance must be continuesly updated by an external timer (via Update).
/// </summary>
/// <example>
/// void Update() { comboInstance.AddTime(Time.deltaTime); }
/// </example>
public class ComboInstance
{
    /// indicates if the combo has already ended
    public bool HasEnded { get; private set; }
    /// <summary>
    /// The combo count of the current instance (each hit in the combo increments the count by one).
    /// </summary>
    public int Count { get; private set; }
    /// <summary> the maximum time between each consecutive hit without dropping the combo (in seconds) </summary>
    public float TimeSinceLastAttack { get; private set; }

    public ComboInstance(float maxTimeBetweenAttacks) {
        this.MaxTimeBetweenAttacks = maxTimeBetweenAttacks;
        HasEnded = false;
        TimeSinceLastAttack = 0;
    }
    // defaults to 2f
    public ComboInstance() : this(2f) { }


    /// <summary> the maximum time between each consecutive hit without dropping the combo (in seconds) </summary>
    public float MaxTimeBetweenAttacks {
        get;
        private set;
    }
    public float TimeRemainingBeforeTimeout {
        get {
            return TimeSinceLastAttack < MaxTimeBetweenAttacks ?
               MaxTimeBetweenAttacks - TimeSinceLastAttack :
              0;
        }
    }



    /// <summary> Use this method in Update() and pass Time.deltaTime
    /// so that the timer will be affected by timeScale </summary>
    /// <param name="timePassed"></param>
    public void AddTime(float timePassed) {
        Debug.Assert(timePassed > 0 && !float.IsNaN(timePassed));
        if (!HasEnded) {
            TimeSinceLastAttack += timePassed;
            if (TimeSinceLastAttack > MaxTimeBetweenAttacks) {
                OnTimerEnd();
            }
        }
    }

    /// <summary> Called when waited too long between attacks </summary>
    private void OnTimerEnd() {
        if (!HasEnded) {
            HasEnded = true;
            TimeSinceLastAttack = 0;
            Debug.Log("ComboInstance ended cuz no attacks have been registered for longer than " + MaxTimeBetweenAttacks + " seconds.");
        }
    }

    /// <summary>
    /// Called everytime an attack happens.
    /// This method resets the timer and increments the combo count
    /// </summary>
    public void IncrementCount() {
        if (HasEnded) { Debug.LogWarning("ComboInstance has already ended, you can't change it after that."); return; }
        Count++;

        // reset the timer
        TimeSinceLastAttack = 0;
    }
}