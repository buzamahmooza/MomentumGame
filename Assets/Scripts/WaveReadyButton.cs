using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveReadyButton : Interactable
{
    [SerializeField]
    private Sprite spritePressed, spriteNotPressed;
    private SpriteRenderer sr;
    private AudioSource audioSource;

    void Awake() {
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start() {
        sr.sprite = spriteNotPressed;
    }

    void OnDisable() { }
    void OnEnable() {
        sr.sprite = spriteNotPressed;
    }

    protected override void DoInteraction() {
        sr.sprite = spritePressed;
        audioSource.Play();
        enabled = false;
    }

    public override string GetPrompt() {
        return "Start next wave";
    }
}
