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
        sr.sprite = spriteNotPressed;
        print("Interactable:    " + interactable);
    }

    protected override void DoInteraction() {
        sr.sprite = spritePressed;
        ar.Play();
        GetComponent<Interactable>().enabled = false;
    }

    public override string GetPrompt() {
        return "Start next wave";
    }
}
