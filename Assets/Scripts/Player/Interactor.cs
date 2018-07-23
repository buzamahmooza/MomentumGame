using System;
using System.Collections;
using System.Collections.Generic;
using InControl;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(Collider2D))]
public class Interactor : MonoBehaviour
{
    [SerializeField] private Text promptText;
    [SerializeField] private LayerMask interactionMask;
    private IEnumerator disableTextCoroutine;

    private void Awake()
    {
        if (!promptText) promptText = GameObject.Find("Interaction prompt text").GetComponent<Text>();
    }


    private void OnTriggerStay2D(Collider2D col)
    {
        var interactable = col.gameObject.GetComponent<Interactable>();
        if (interactable == null)
        {
            return;
        }

        promptText.enabled = true;
        promptText.text = interactable.GetPrompt();

        // disable the previouse coroutine, this check prevents flickering text
        if (disableTextCoroutine != null) StopCoroutine(disableTextCoroutine);
        disableTextCoroutine = WaitAndDisableText(3);
        StartCoroutine(disableTextCoroutine);

        if (CrossPlatformInputManager.GetButtonDown("Fire1") || InputManager.ActiveDevice.Action3.IsPressed)
        {
            if (interactable.enabled)
            {
                interactable.OnInteract();
                DisableText();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        promptText.enabled = true;
        var interactable = col.gameObject.GetComponent<Interactable>();
        if (interactable == null)
        {
            return;
        }

        DisableText();
    }

    private IEnumerator WaitAndDisableText(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        DisableText();
    }
    private void DisableText()
    {
        promptText.text = "";
        promptText.enabled = false;
    }
}
