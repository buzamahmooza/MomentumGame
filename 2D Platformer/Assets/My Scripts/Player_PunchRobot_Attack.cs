using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(PlayerMove))]
public class Player_PunchRobot_Attack : MonoBehaviour {

    bool slamming = false,
        punching = false;
    Animator anim;
    [SerializeField] BoxCollider2D /*punchTrigger,*/ slamTrigger;
    PlayerMove playerMove;
    private float animSpeed=1;
    public static bool reachedSlamPeak = false;

    private void Awake() {
        playerMove = GetComponent<PlayerMove>();
        anim = GetComponent<Animator>();
        //punchTrigger = transform.Find("HitBox_Punch").gameObject.GetComponent<BoxCollider2D>();
        //slamTrigger = transform.Find("HitBox_Slam").GetComponent<BoxCollider2D>();
    }

    private void Start() {
        slamTrigger.enabled = false;
        //punchTrigger.enabled = false;
        animSpeed = anim.speed;
        //Debug.Assert(slamTrigger != null && punchTrigger != null);
    }

    void Update () {
        if(!(punching || slamming)){
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKey(KeyCode.F) || CrossPlatformInputManager.GetButtonDown("Fire1"))
                TriggerAttack();
        }
        UpdateAnimatorParams();

        //When landing a slam on the ground, continue playing animation
        if (anim.GetBool("Slamming") && playerMove.isGrounded) {
            anim.speed = animSpeed;
            //Debug.Log("Reset animation speed back to normal after slam completed");
        }
    }

    private void TriggerAttack() {
        punching = slamming = false;        
        if (playerMove.CheckIfGrounded()) {
            punching = true;
            UpdateAnimatorParams();
        }
        else if (!playerMove.CheckIfGrounded()) {
            slamming = true;
            UpdateAnimatorParams();
        }
    }

    public void ReachedSlamPeak() {
        reachedSlamPeak = true;
        anim.speed = 0;
    }

    public void Attack_Punch()
    {
        //punchTrigger.enabled = true;
    }

    public void Attack_Slam()
    {
        slamTrigger.enabled = true;
    }

    public void SlamEnded() {
        slamming = false;
        anim.speed = animSpeed;
        slamTrigger.enabled = true;
        UpdateAnimatorParams();
    }

    public void PunchEnded() {
        punching = false;
        UpdateAnimatorParams();
    }

    private void UpdateAnimatorParams()
    {
        anim.SetBool("Punching", punching);
        anim.SetBool("Slamming", slamming);
    }

}
