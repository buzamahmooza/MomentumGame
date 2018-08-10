using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _spawnPointsParent;
    [SerializeField] private GameObject[] _enemies;
    [SerializeField] private bool _useSpawnPointsParent = false;

    /// <summary> Maximum allowed number of enemies to be active in the scene at one time </summary>
    [SerializeField] private int _maxAllowedActiveEnemies = 8;

    /// <summary> the random range of delay between spawns. X: minimum, Y: maximum </summary>
    [SerializeField] private Vector2 _delayBetweenSpawns = new Vector2(0.5f, 2f);

    /// <summary> An interactable that will trigger spawning enemies </summary>
    [SerializeField] private Interactable _interactable;

    [SerializeField] private int _defaultWaveSize = 10;

    private Transform[] _spawnPoints;
    private GameObject _player;
    private AudioSource _audioSource;

    /// <summary> the number of enemies that still need to be spawned in this wave </summary>
    private int _remainingInWave = 0;

    private int _totalSpawnedFromStart = 0;


    // subscribe / unsubscribe to the interaction event
    private void OnEnable()
    {
        if (_interactable != null)
            _interactable.InteractEvent += SpawnWave;
    }

    private void OnDisable()
    {
        if (_interactable != null)
            _interactable.InteractEvent -= SpawnWave;
    }

    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_useSpawnPointsParent)
            _spawnPoints = _spawnPointsParent.GetComponentsInChildren<Transform>();

        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // if no wave controller exists, automatically start spawning, othwerise just wait for the event
        if (_interactable == null)
            Invoke("SpawnEnemy", SpawnDelay);
    }

    private void SpawnEnemy()
    {
        // if player is dead or too many enemies exist
        if (_player == null || GameManager.PlayerHealth.IsDead || _maxAllowedActiveEnemies <= LivingEnemies)
        {
            print("Not gonna spawn cuz too many enemies are active, or player is dead");
            Invoke("SpawnEnemy", SpawnDelay);
            return;
        }

        // the actual spawn part
        GameObject enemy = _enemies[Random.Range(0, _enemies.Length)]; //choose random enemy type
        Transform spawnTransform = _spawnPoints[Random.Range(0, _spawnPoints.Length)]; //choose random spawnpoint
        Instantiate(enemy, spawnTransform.position, Quaternion.identity);
        _audioSource.Play();

        _totalSpawnedFromStart++;
        _remainingInWave--;

        if (_remainingInWave > 0)
        {
            Invoke("SpawnEnemy", SpawnDelay);
        }
        else
        {
            WaveEnd();
        }
    }

    private void WaveEnd()
    {
        print("WaveEnd()");
        // re-enable the interactable
        if (_interactable)
            _interactable.enabled = true;
    }

    public void SpawnWave()
    {
        // do not spawn next wave until current wave is over
        int enemiesAlive = LivingEnemies;
        print("enemies alive = " + enemiesAlive);
        if (_remainingInWave > 0 && enemiesAlive > 0)
            return;
        SpawnWave(_defaultWaveSize, _maxAllowedActiveEnemies);
    }

    public void SpawnWave(int waveSize, int newMaxSpawnedEnemies)
    {
        // disable the interactable so the player won't be able to spam it
        _interactable.enabled = false;

        Debug.Log(string.Format("SpawnEnemy(waveSize = {0}, newMaxSpawnedEnemies = {1})", waveSize,
            newMaxSpawnedEnemies));
        this._maxAllowedActiveEnemies = newMaxSpawnedEnemies;
        this._remainingInWave = waveSize;
        Invoke("SpawnEnemy", 0);
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

    private float SpawnDelay
    {
        get { return Random.Range(_delayBetweenSpawns.x, _delayBetweenSpawns.y); }
    }
}