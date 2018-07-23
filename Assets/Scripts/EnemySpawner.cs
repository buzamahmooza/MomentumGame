using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{

    [SerializeField] private GameObject spawnPointsParent;
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private bool useSpawnPointsParent = false;
    [SerializeField] private int maxSpawnedEnemies = 8;
    [SerializeField] private Vector2 delayBetweenSpawns = new Vector2(0.5f, 2f);
    /// <summary>
    /// An interactable that will trigger spawning enemies
    /// </summary>
    [SerializeField] private Interactable interactable;

    private Transform[] spawnPoints;
    private GameObject player;
    private AudioSource audioSource;
    private int remainingWaveSize = 0;
    private int totalSpawnedFromStart = 0;

    // subscribe / unsubscribe to the interaction event
    private void OnEnable() {
        if (interactable != null)
            interactable.InteractEvent += SpawnWave;
    }
    private void OnDisable() {
        if (interactable != null)
            interactable.InteractEvent -= SpawnWave;
    }

    private void Awake() {
        player = GameObject.FindGameObjectWithTag("Player");
        if (useSpawnPointsParent) {
            spawnPoints = spawnPointsParent.GetComponentsInChildren<Transform>();
        }
        audioSource = GetComponent<AudioSource>();
    }
    private float SpawnDelay { get { return Random.Range(delayBetweenSpawns.x, delayBetweenSpawns.y); } }

    // Use this for initialization
    private void Start() {
        // if no wave controller exists, automatically start spawning, othwerise just wait for the event
        if (interactable == null)
            Invoke("SpawnEnemy", SpawnDelay);
    }

    private void SpawnEnemy() {
        if (player == null || GameManager.PlayerHealth.IsDead || // if player is dead
            maxSpawnedEnemies <= GameObject.FindGameObjectsWithTag("Enemy").Length // or too many enemies exist
        ) {
            return;
        }

        GameObject enemy = enemies[(int)Random.Range(0, enemies.Length)]; //choose random enemy type
        Transform spawnTransform = spawnPoints[(int)Random.Range(0, spawnPoints.Length)]; //choose random spawnpoint
        audioSource.Play();
        Instantiate(enemy, spawnTransform.position, Quaternion.identity);

        totalSpawnedFromStart++;
        remainingWaveSize--;

        if (remainingWaveSize > 0) {
            Invoke("SpawnEnemy", SpawnDelay);
        } else {
            //wave ended
            WaveEnd();
        }
    }

    private void WaveEnd() {
        print("WaveEnd()");
        // re-enable the interactable
        interactable.enabled = true;
        if (interactable)
            interactable.enabled = true;
    }

    public void SpawnWave() {
        // do not spawn next wave until current wave is over
        if (remainingWaveSize > 0 && GameObject.FindGameObjectsWithTag("Enemy").Length != 0)
            return;
        SpawnWave(10, maxSpawnedEnemies);
    }
    public void SpawnWave(int waveSize, int newMaxSpawnedEnemies) {
        // disable the interactable so the player won't be able to spam it
        interactable.enabled = false;

        Debug.Log("SpawnEnemy(waveSize = " + waveSize + ", newMaxSpawnedEnemies = " + newMaxSpawnedEnemies + ")");
        this.maxSpawnedEnemies = newMaxSpawnedEnemies;
        this.remainingWaveSize = waveSize;
        Invoke("SpawnEnemy", 0);
    }
}
