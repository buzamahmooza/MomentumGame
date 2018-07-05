using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : Health
{
    public event Action PlayerDeathEvent;

    public override void Die() {
        base.Die();
        if (PlayerDeathEvent != null)
            PlayerDeathEvent();
        base.walker.BlockMoveInput = true;
        var restartText = GameObject.Find("RestartText").GetComponent<Text>();
        if (restartText) restartText.enabled = true;
        Debug.Log(gameObject.name + " died");
        gameObject.SetActive(false);
    }
}
