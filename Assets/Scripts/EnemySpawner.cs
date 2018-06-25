using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    public GameObject spawnPointsParent;
    public GameObject[] enemies;
    public bool useSpawnPointsParent = false;
    public int maxSpawnedEnemies = 8;
    public Vector2 delayBetweenSpawns = new Vector2(0.5f, 2f);

    private Transform[] spawnPoints;
    private GameObject player;
    private AudioSource audioSource;
    private int remainingWaveSize = 10;
    private int totalSpawnedFromStart = 0;

    private void Awake() {
        player = GameObject.FindGameObjectWithTag("Player");
        if (useSpawnPointsParent) {
            int length = spawnPointsParent.transform.childCount;
            spawnPoints = new Transform[length];

            for (int i = 0; i < spawnPoints.Length; i++) {
                spawnPoints[i] = spawnPointsParent.transform.GetChild(i);
            }
        }
        audioSource = GetComponent<AudioSource>();
    }
    private float SpawnDelay {
        get {
            return Random.Range(delayBetweenSpawns.x, delayBetweenSpawns.y);
        }
    }

    // Use this for initialization
    private void Start() {
        InvokeRepeating("SpawnEnemy", SpawnDelay, SpawnDelay);
    }

    private void SpawnEnemy() {
        if (player == null || player.GetComponent<HealthScript>().isDead || // if player is dead
            maxSpawnedEnemies <= GameObject.FindGameObjectsWithTag("Enemy").Length) // or too many enemies exist
            return;

        GameObject enemy = enemies[(int)Random.Range(0, enemies.Length)]; //choose random enemy type
        Transform spawnPoint = spawnPoints[(int)Random.Range(0, spawnPoints.Length)]; //choose random spawnpoint
        audioSource.Play();
        Instantiate(enemy, spawnPoint.position, Quaternion.identity);
        totalSpawnedFromStart++;
        remainingWaveSize--;
    }

    public void SpawnWave(int waveSize, int newMaxSpawnedEnemies) {
        this.maxSpawnedEnemies = newMaxSpawnedEnemies;
        InvokeRepeating("SpawnEnemy", SpawnDelay, SpawnDelay);
    }
}
