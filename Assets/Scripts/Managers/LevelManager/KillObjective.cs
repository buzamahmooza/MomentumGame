using System;
using System.Collections;
using System.Linq;
using Actors;
using Actors.Enemy;
using UnityEngine;
using UnityEngine.Serialization;

public class KillObjective : RoomObjective
{
    [FormerlySerializedAs("_requiredKills")] [SerializeField] [Range(0, 100)] private int m_requiredKills = 25;
    private int m_killsGotten = 0;


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
        m_killsGotten++;
        print($"OnEnemyKill(): killsGotten: {m_killsGotten}");

        if (m_killsGotten >= m_requiredKills)
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
        return string.Format("Eliminate {0} enemies.", m_requiredKills);
    }
}