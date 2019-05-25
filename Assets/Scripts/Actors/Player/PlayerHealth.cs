using System;
using UnityEngine;
using UnityEngine.UI;

namespace Actors.Player
{
    public class PlayerHealth : Health
    {
        /// <inheritdoc />
        public override void Die()
        {
            base.Die();
            Walker.IsMoveInputBlocked = true;

            Text restartText = GameObject.Find("RestartText").GetComponent<Text>();
            if (restartText) restartText.enabled = true;

            Debug.Log(gameObject.name + " died");
            Anim.SetBool("Dead", true);

            //disable all scriptstry 
            GrappleHook grappleHook = GetComponent<GrappleHook>();
            if (grappleHook) grappleHook.EndGrapple();

            SpriteRenderer.color = Color.white;
        
        
        
            // disabling scripts (super buggy)
            try
            {
                Destroy(GetComponent<GrappleHook>());
                Destroy(GetComponent<PlayerAttack>());
//            Destroy(GetComponent<PlayerMove>());
                Destroy(GetComponent<Shooter>());
//            Destroy(GetComponent<Health>());
            }
            catch (Exception e)
            {
                Debug.LogError("Caught: " + e);
            }

            foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
                try
                {
                    script.enabled = false;
//                Destroy(script);
                }
                catch (Exception e)
                {
                    Debug.LogError("Caught: " + e);
                }
        }
    }
}