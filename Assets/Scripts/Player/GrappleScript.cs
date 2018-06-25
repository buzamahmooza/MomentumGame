using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleScript : MonoBehaviour
{

    public Transform gun;
    public LayerMask mask;
    public float speed = 15;
    public float maxGrappleRange = 300;

    [HideInInspector] public Vector3 target;
    [HideInInspector] public GameObject grabbedObj;
    private Vector3 targetPointOffset;

    private AimInput inputAimDirectionScript;
    private PlayerMove playerMove;
    private LineRenderer LR;
    private Animator m_Anim;
    public bool m_Flying = false, m_Pulling = false;
    private int Origin = 0, End = 1;
    public bool releaseGrappleOnInputRelease;

    private void Awake() {
        if (mask == 0) mask = LayerMask.NameToLayer("Floor");
        LR = GetComponent<LineRenderer>();
        inputAimDirectionScript = GetComponent<AimInput>();
        playerMove = GetComponent<PlayerMove>();
        m_Anim = GetComponent<Animator>();
    }

    private void Update() {
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(1)) && !m_Flying)
            FindTarget();

        if (m_Flying)
            Fly();
        else if (m_Pulling)
            Pull(grabbedObj);
        else
            EndGrapple();

        if ((Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.LeftShift)) && releaseGrappleOnInputRelease)
            EndGrapple();
    }

    private void FindTarget() {
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(gun.position, inputAimDirectionScript.AimDirection, maxGrappleRange, mask) as RaycastHit2D[]) {
            bool grappleConditions = hit && hit.collider != null &&
                //hit.collider.gameObject.GetComponent<Rigidbody2D>() != null &&
                hit.collider.gameObject != gameObject;
            bool pullConditions = hit.collider.gameObject.GetComponent<HealthScript>() != null &&
                hit.collider.attachedRigidbody != null;


            bool slamming = m_Anim.GetBool("Slamming");
            if (!slamming) {
                target = hit.point;
                LR.enabled = true;
                LR.SetPosition(0, gun.position);
                LR.SetPosition(1, target);

                if (pullConditions) {
                    grabbedObj = hit.collider.gameObject;
                    targetPointOffset = hit.point - (Vector2)grabbedObj.transform.position;
                    m_Pulling = true;
                    return;
                } else if (grappleConditions) {
                    target = hit.point;
                    playerMove.blockMoveInput = true;
                    m_Flying = true;
                    return;
                }
            }
        }
    }

    private void Fly() {
        playerMove.rb.velocity = Vector3.zero;
        float smoother = speed * Time.deltaTime / Vector2.Distance(transform.position, target);
        transform.position = Vector3.Lerp(transform.position, target, smoother);
        LR.SetPosition(Origin, gun.position);
        LR.SetPosition(End, target);

        //If reached wall
        if (Vector2.Distance(transform.position, target) < 0.5f)
            EndGrapple();
    }

    private void Pull(GameObject obj) {
        if (obj == null) return;
        float smoother = speed * Time.deltaTime / Vector2.Distance(transform.position, obj.transform.position);
        Rigidbody2D objRB = obj.GetComponent<Rigidbody2D>();
        objRB.velocity = Vector2.zero;
        objRB.freezeRotation = true;
        obj.transform.position = Vector3.Lerp(obj.transform.position, gun.position, smoother);
        LR.SetPosition(Origin, gun.position);
        LR.SetPosition(End, obj.transform.position + targetPointOffset);

        //If reached wall
        if (Vector2.Distance(transform.position, obj.transform.position) < 0.5f)
            EndGrapple();
    }

    public void EndGrapple() {
        LR.enabled = false;
        m_Flying = false;
        m_Pulling = false;
        playerMove.blockMoveInput = false;
        target = transform.position;
        grabbedObj = null;
    }
}
