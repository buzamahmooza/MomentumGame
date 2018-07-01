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
        GetComponent<PlayerMove>().blockMoveInput = true;
        print(gameObject.name + " died");
        gameObject.SetActive(false);
    }
}
