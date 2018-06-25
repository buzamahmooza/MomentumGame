using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Interact : MonoBehaviour
{
    private Text promptText;
    private CapsuleCollider2D capsCol;
    [SerializeField] private LayerMask interactionMask;

    private void Awake() {
        promptText = GameObject.Find("Prompt text").GetComponent<Text>();
        capsCol = GetComponent<CapsuleCollider2D>();
    }

    private void Update() {
        if (Input.GetButton("Fire1")) {
            print("Fire1 pressed");
            var hits = Physics2D.OverlapCircleAll(capsCol.bounds.center, 2);
            foreach (var col in hits) {
                promptText.enabled = true;
                print("Detecting object:    " + col.gameObject.name);
                var interactable = col.gameObject.GetComponent<Interactable>();
                if (interactable == null) {
                    Debug.LogWarning("Interactable is null. " + col.gameObject.name);
                    continue;
                }

                print("Supposed to be working here");
                promptText.text = interactable.GetPrompt();

                interactable.OnInteract();
                return; // return after one interactable item, we only want a maximum of one to be active at a time.
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D col) {
        promptText.enabled = true;
        print("Detecting object:    " + col.gameObject.name);
        var interactable = col.gameObject.GetComponent<Interactable>();
        if (interactable == null) {
            Debug.LogWarning("Interactable is null. " + col.gameObject.name);
            return;
        }

        print("Supposed to be working here");
        promptText.text = interactable.GetPrompt();

        //interactable.OnInteract();
    }

    private void OnTriggerExit2D(Collider2D col) {
        promptText.enabled = true;
        print("Detecting object:    " + col.gameObject.name);
        var interactable = col.gameObject.GetComponent<Interactable>();
        if (interactable == null) {
            Debug.LogWarning("Interactable is null. " + col.gameObject.name);
            return;
        }

        DisableText();
    }

    private void DisableText() {
        promptText.enabled = false;
    }
}
