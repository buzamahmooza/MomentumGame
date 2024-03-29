﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Actors.Enemy;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] m_enemies;

    /// <summary> Maximum allowed number of enemies to be active in the scene at one time </summary>
    [SerializeField] private int m_maxAllowedActiveEnemies = 8;

    /// <summary> the random range of delay between spawns. X: minimum, Y: maximum </summary>
    [SerializeField] private Vector2 m_delayBetweenSpawns = new Vector2(0.5f, 2f);

    /// <summary> An interactable that will trigger spawning enemies </summary>
    [SerializeField] public Interactable Interactable;

    [HideInInspector] public Transform SpawnPointsParent;

    [SerializeField] private int m_defaultWaveSize = 10;

    private GameObject m_player;
    private AudioSource m_audioSource;

    /// <summary> the number of enemies that still need to be spawned in this wave </summary>
    private int m_remainingInWave = 0;

    public readonly HashSet<GameObject> EnemySet = new HashSet<GameObject>();

    public EnemySpawner()
    {
        TotalSpawnedFromStart = 0;
    }

    /// <summary>
    /// An ActionEvent that is fired when an enemy is spawned, with the enemy passed with it.
    /// </summary>
    public static Action<Enemy> OnEnemySpawn;

    public Action OnWaveEnd;


    // subscribe / unsubscribe to the interaction event
    private void OnEnable()
    {
        if (Interactable != null)
            Interactable.InteractEvent += TryToSpawnWave;
    }

    private void OnDisable()
    {
        if (Interactable != null)
            Interactable.InteractEvent -= TryToSpawnWave;
    }

    private void Awake()
    {
        m_player = GameObject.FindGameObjectWithTag("Player");

        m_audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// This method should be called when the player enters the room
    /// </summary>
    public void InitializeSpawner()
    {
        EndWave();
        CancelInvoke(nameof(SpawnEnemyIfPossible));
        // if no wave controller exists, automatically start spawning, othwerise just wait for the event
        if (Interactable != null)
        {
            Interactable.InteractEvent += SpawnWave;
        }
        else
        {
            print("Spawning wave");
            SpawnWave();
        }
    }

    private void Start()
    {
        InitializeSpawner();
    }

    /// <summary>
    /// spawns an enemy if the conditions are met, also increments all the spawn counters and decrements _remainingWaveSize
    /// </summary>
    private void SpawnEnemyIfPossible()
    {
        // if player is dead or too many enemies exist
        if (m_player == null || GameComponents.PlayerHealth.IsDead || m_maxAllowedActiveEnemies <= LivingEnemies)
        {
            print("Not gonna spawn cuz too many enemies are active, or player is dead");
            Invoke(nameof(SpawnEnemyIfPossible), SpawnDelay);
            return;
        }

        // the actual spawn part
        GameObject enemy = CreateEnemy();
        if (OnEnemySpawn != null)
            OnEnemySpawn(enemy.GetComponent<Enemy>());

        TotalSpawnedFromStart++;
        m_remainingInWave--;

        if (m_remainingInWave > 0)
        {
            Invoke(nameof(SpawnEnemyIfPossible), SpawnDelay);
        }
        else
        {
            EndWave();
        }
    }

    private GameObject CreateEnemy()
    {
        // TODO: make this lograithmic (less likely to  spawn from the top) and put hardest enemies at the end of the array
        GameObject enemyPrefab = m_enemies[Random.Range(0, m_enemies.Length)]; //choose random enemy type

        if (enemyPrefab == null)
            throw new NullReferenceException("EnemySpawner: A slot in the enemy list contians a null enemy.");

        //choose random spawnpoint
        Transform spawnTransform = Utils.GetRandomElement(SpawnPointsParent.GetComponentsInChildren<Transform>());

        GameObject enemy = Instantiate(enemyPrefab, spawnTransform.position, Quaternion.identity);
        EnemySet.Add(enemy);

        m_audioSource.Play();
        return enemyPrefab;
    }

    private void EndWave()
    {
        print("WaveEnd()");

        // re-enable the interactable
        if (Interactable)
            Interactable.enabled = true;
        else
            TryToSpawnWave();

        if (OnWaveEnd != null)
            OnWaveEnd();
    }

    /// <summary>
    /// Spawns a wave IF the conditions are met: That the current wave is over
    /// </summary>
    public void TryToSpawnWave()
    {
        // do not spawn next wave until current wave is over
        if (m_remainingInWave > 0 && LivingEnemies > 0)
            return;

        SpawnWave();
    }

    public void SpawnWave()
    {
        SpawnWave(m_defaultWaveSize, m_maxAllowedActiveEnemies);
    }

    public void SpawnWave(int waveSize, int newMaxSpawnedEnemies)
    {
        // disable the interactable so the player won't be able to spam it
        if (Interactable != null)
            Interactable.enabled = false;

        Debug.Log(string.Format("SpawnEnemy(waveSize = {0}, newMaxSpawnedEnemies = {1})", waveSize,
            newMaxSpawnedEnemies));
        this.m_maxAllowedActiveEnemies = newMaxSpawnedEnemies;
        this.m_remainingInWave = waveSize;
        Invoke(nameof(SpawnEnemyIfPossible), 0);
    }

    private static int LivingEnemies
    {
        get
        {
            return FindObjectsOfType<EnemyHealth>()
                .Where(enemyHealth => !enemyHealth.IsDead && enemyHealth.gameObject.activeInHierarchy)
                .ToArray().Length;
        }
    }

    private float SpawnDelay => Random.Range(m_delayBetweenSpawns.x, m_delayBetweenSpawns.y);

    public int TotalSpawnedFromStart { get; private set; }


    private void OnGUI()
    {
#if UNITY_EDITOR
        if (GUI.Button(new Rect(x: 100, y: 100, width: 100, height: 20), "SpawnWave"))
            SpawnWave();
        if (GUI.Button(new Rect(x: 100, y: 120, width: 100, height: 20), "TryToSpawnWave"))
            TryToSpawnWave();
#endif
    }
}