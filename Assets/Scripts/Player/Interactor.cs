using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class Interactor : MonoBehaviour
{
    [SerializeField] private Text promptText;
    [SerializeField] private LayerMask interactionMask;

    private void Awake() {
        if (!promptText) promptText = GameObject.Find("Interaction prompt text").GetComponent<Text>();
    }


    private void OnTriggerStay2D(Collider2D col) {
        var interactable = col.gameObject.GetComponent<Interactable>();
        if (interactable == null) {
            return;
        }

        promptText.enabled = true;
        promptText.text = interactable.GetPrompt();
        StartCoroutine(WaitAndDisableText(3));
        if (Input.GetButtonDown("Fire1")) {
            interactable.OnInteract();
            DisableText();
        }
    }

    private void OnTriggerExit2D(Collider2D col) {
        promptText.enabled = true;
        var interactable = col.gameObject.GetComponent<Interactable>();
        if (interactable == null) {
            return;
        }

        DisableText();
    }

    private IEnumerator WaitAndDisableText(float seconds) {
        yield return new WaitForSeconds(seconds);
        DisableText();
    }
    private void DisableText() {
        promptText.text = "";
        promptText.enabled = false;
    }
}
