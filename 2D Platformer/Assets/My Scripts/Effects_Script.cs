using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effects_Script : MonoBehaviour {
    private float duration;
    Animator anim;

    private void Awake() {
        anim = GetComponent<Animator>();
        duration = anim.GetCurrentAnimatorClipInfo(anim.layerCount - 1)[anim.layerCount - 1].clip.length;
    }

    void Start () {
        Destroy(gameObject, duration);
    }
}
