using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

public class CameraController : MonoBehaviour
{
    public Transform target;
    [SerializeField] bool boundsX, boundsY;
    public Vector2 minCamPos, maxCamPos;


    [SerializeField] [Range(0.01f, 1f)] private float _smooth = 0.15f;

    private Vector3 offset;

    private float zDist;

    private Fisheye fisheye;
    [SerializeField] private float fisheyeValue = 0.0f;
    [SerializeField] [Range(0, 20)] float fisheyeSmooth = 0.3f;

    private void Awake()
    {
        GetComponent<Animator>();
        fisheye = Camera.main.GetComponent<Fisheye>();
    }

    // Use this for initialization
    private void Start()
    {
        if (!target)
        {
            target = GameManager.Player.transform;
        }


        zDist = transform.position.z - target.position.z;
        offset = transform.position - target.position;
    }

    private void LateUpdate()
    {
        ApproachTarget();

        transform.position =
            new Vector3(
                !boundsX ? transform.position.x : Mathf.Clamp(transform.position.x, minCamPos.x, maxCamPos.x),
                !boundsY ? transform.position.y : Mathf.Clamp(transform.position.y, minCamPos.y, maxCamPos.y),
                zDist
            );

        //UpdateFisheye();
    }

    private void UpdateFisheye()
    {
        // if fisheyeValue ~== zero
        if (Math.Abs(fisheyeValue) < 0.01f)
        {
            // end fisheye
            fisheye.enabled = false;
            fisheyeValue = 0;
        }
        else
        {
            Mathf.Lerp(fisheye.strengthX, fisheyeValue, Time.time * fisheyeSmooth);
            // decrease fisheyeValue
            fisheyeValue -= Time.deltaTime;
        }

        fisheye.strengthX = fisheyeValue;
        fisheye.strengthY = fisheyeValue;
    }

    public void DoFisheye(float intensity)
    {
        if (!fisheye)
        {
            Debug.LogError("Fisheye is null");
            return;
        }
        //animator.SetTrigger("Fisheye");
        //return;

        fisheye.enabled = true;
        // reset fields (we don't want a sudden change in lense intensity, so we start from zero and Lerp)
        fisheye.strengthX = 0;
        fisheye.strengthY = 0;

        fisheyeValue = intensity;
    }

    private void ApproachTarget()
    {
        Vector3 offset2D = new Vector3(offset.x, offset.y, 0);
        Vector3 targetPos2D = new Vector3(target.position.x, target.position.y, zDist);

        // Transition to this new position
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos2D + offset2D,
            _smooth * Mathf.Exp(Vector2.Distance(targetPos2D, transform.position))
        );
    }

    public Vector3 LookAheadPos
    {
        get { return target ? (target.position - transform.position) : Vector3.zero; }
    }
}