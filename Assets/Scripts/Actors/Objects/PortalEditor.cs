﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Manages a pair of teleporters
/// Note: Teleporters must be face opposite directions (one should be facing left and the other facing right)
/// (i.e if you go in one facing right, you must come out the other facing right)
/// </summary>
[ExecuteInEditMode]
public class PortalEditor : MonoBehaviour
{
    /// <summary> portal </summary>
    [SerializeField] private Portal _portal1, _portal2;

    /// <summary>
    /// indicates the current status of the portal locations (if there's an error or not)
    /// </summary>
    private Color _statusColor = Color.green;


    void Start()
    {
        if (_portal1 == null || _portal2 == null)
        {
            _portal1 = transform.GetChild(0).GetComponent<Portal>();
            _portal2 = transform.GetChild(1).GetComponent<Portal>();
        }

        _portal1.OtherPortal = _portal2;
        _portal2.OtherPortal = _portal1;
    }

    void Update()
    {
        // draw line between them to make it clear for the user
        Debug.DrawLine(_portal1.transform.position, _portal2.transform.position, _statusColor);

        // draw a line to the spawn position for each portal
        Debug.DrawLine(
            new Vector2(_portal1.transform.position.x, _portal1.SpawnTransform.position.y),
            _portal1.SpawnTransform.position, Color.blue
        );
        Debug.DrawLine(
            new Vector2(_portal2.transform.position.x, _portal2.SpawnTransform.position.y),
            _portal2.SpawnTransform.position, Color.blue
        );

        // if the left
        if (IsPortalBlockedRight(_portal1.gameObject) && IsPortalBlockedRight(_portal2.gameObject) ||
            IsPortalBlockedLeft(_portal1.gameObject) && IsPortalBlockedLeft(_portal2.gameObject) ||
            IsPortalSpawnBlocked(_portal1) || IsPortalSpawnBlocked(_portal2))
        {
            _statusColor = Color.red;
        }
        else
        {
            _statusColor = Color.green;

            if (IsPortalBlockedRight(_portal1.gameObject) || IsPortalBlockedLeft(_portal2.gameObject))
            {
                FaceLeft(_portal1.transform);
                FaceRight(_portal2.transform);
            }
            else if (IsPortalBlockedRight(_portal2.gameObject) || IsPortalBlockedLeft(_portal1.gameObject))
            {
                FaceLeft(_portal2.transform);
                FaceRight(_portal1.transform);
            }
        }
    }

    static void FaceRight(Transform transform)
    {
        FaceDirection(transform, true);
    }

    static void FaceLeft(Transform transform)
    {
        FaceDirection(transform, false);
    }

    private static void FaceDirection(Transform transform, bool right)
    {
        transform.localScale = new Vector3(
            right ? 1 : -1,
            transform.localScale.y,
            transform.localScale.z
        );
    }

    private static bool IsPortalSpawnBlocked(Portal portal)
    {
        return Physics2D.CircleCast(portal.SpawnTransform.position, 0.5f, Vector2.up, 0.5f, LayerMask.GetMask("Floor"));
    }

    /// <summary>
    /// checks if something is blocking the object from the right
    /// </summary>
    /// <param name="portal"></param>
    /// <returns></returns>
    private RaycastHit2D IsPortalBlockedRight(GameObject portal)
    {
        return Physics2D.Raycast(
            portal.transform.position, portal.transform.right, 1,
            LayerMask.GetMask("Floor")
        );
    }

    private RaycastHit2D IsPortalBlockedLeft(GameObject portal)
    {
        return Physics2D.Raycast(
            portal.transform.position, -portal.transform.right, 1,
            LayerMask.GetMask("Floor")
        );
    }
}