using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveReadyButton : Interactable
{
    [SerializeField]
    private Sprite spritePressed, spriteNotPressed;
    private SpriteRenderer sr;
    private AudioSource ar;

    private void Awake() {
        sr = GetComponent<SpriteRenderer>();
        ar = GetComponent<AudioSource>();
    }

    private void Start() {
        var interactable = GetComponent<Interactable>();
        print("Interactable:    " + interactable);
    }

    public override void OnInteract() {
        sr.sprite = spritePressed;
        ar.Play();
    }

    public override string GetPrompt() {
        return "Start next wave";
    }
}
