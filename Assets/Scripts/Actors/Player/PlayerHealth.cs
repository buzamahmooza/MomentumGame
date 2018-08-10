using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PlayerHealth : Health
{
    /// <inheritdoc />
    public override void Die()
    {
        base.Die();
        walker.BlockMoveInput = true;

        Text restartText = GameObject.Find("RestartText").GetComponent<Text>();
        if (restartText) restartText.enabled = true;

        Debug.Log(gameObject.name + " died");
        _anim.SetBool("Dead", true);

        //disable all scriptstry 
        GetComponent<GrappleHook>().EndGrapple();

        SpriteRenderer.color = Color.white;

        Destroy(GetComponent<Walker>());
        Destroy(GetComponent<Shooter>());
        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
           Destroy(script);
    }
}