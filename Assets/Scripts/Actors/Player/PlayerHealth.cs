﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
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
        var grappleHook = GetComponent<GrappleHook>();
        if (grappleHook) grappleHook.EndGrapple();

        SpriteRenderer.color = Color.white;

        try
        {
            Destroy(GetComponent<GrappleHook>());
            Destroy(GetComponent<PlayerAttack>());
//            Destroy(GetComponent<PlayerMove>());
            Destroy(GetComponent<Shooter>());
//            Destroy(GetComponent<Health>());
        }
        catch (Exception e)
        {
            Debug.LogError("Caught: " + e);
        }

        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
            try
            {
                script.enabled = false;
//                Destroy(script);
            }
            catch (Exception e)
            {
                Debug.LogError("Caught: " + e);
            }
    }
}