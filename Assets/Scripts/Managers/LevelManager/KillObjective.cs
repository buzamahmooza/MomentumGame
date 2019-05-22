using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class KillObjective : RoomObjective
{
    [SerializeField] [Range(0, 100)] private int _requiredKills = 25;
    private int _killsGotten = 0;


    void Start()
    {
        EnemySpawner.OnEnemySpawn += SubscribeToEnemyDeath;
    }

    private void SubscribeToEnemyDeath(Enemy enemy)
    {
        enemy.GetComponent<Health>().OnDeath += OnEnemyKill;
        
//        Debug.Log("Subscribed to spawned enemy: " + enemy.name + " " +
//                  string.Join("\n",
//                      enemy.GetComponent<Health>().OnDeath.GetInvocationList()
//                          .Select(x => x.GetInvocationList())
//                  ));
    }

    public void OnEnemyKill()
    {
        _killsGotten++;
        print($"OnEnemyKill(): killsGotten: {_killsGotten}");

        if (_killsGotten >= _requiredKills)
        {
            if (!IsObjectiveComplete)
                if (OnObjectiveComplete != null)
                    OnObjectiveComplete();

            IsObjectiveComplete = true;
            enabled = false;
        }
    }

    public override string GetObjectiveMessage()
    {
        return string.Format("Eliminate {0} enemies.", _requiredKills);
    }
}