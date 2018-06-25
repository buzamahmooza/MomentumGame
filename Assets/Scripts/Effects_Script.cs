using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// idk what this does, please help me
/// </summary>
public class Effects_Script : MonoBehaviour {
    private float duration;
    private Animator anim;

    private void Awake() {
        anim = GetComponent<Animator>();
        duration = anim.GetCurrentAnimatorClipInfo(anim.layerCount - 1)[anim.layerCount - 1].clip.length;
    }

    private void Start () {
        Destroy(gameObject, duration);
    }
}
