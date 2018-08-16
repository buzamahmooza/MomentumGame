using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class RoomBuilder : MonoBehaviour
{
    [SerializeField] private GameObject[] _rooms;
    [SerializeField] private GameObject _startingRoom;

    /// <summary>
    /// the last room that was created, always the furthest
    /// </summary>
    private GameObject _latestRoom;

    public RoomObjective CurrentObjective { get; private set; }

    void Start()
    {
        Debug.Assert(_rooms.Length > 0);

        if (_startingRoom == null)
        {
            _startingRoom = Instantiate(_rooms[0], transform.position, Quaternion.identity, this.transform);
            print("Starting room is null, creating room");
        }

        _latestRoom = _startingRoom;
        SetupRoom(_latestRoom);
    }

    public GameObject BuildRoom()
    {
        return BuildRoom(Utils.GetRandomElement(_rooms));
    }

    /// <summary>
    /// Instantiates the room to the right of the latest room built.
    /// Sets _latestRoom = to the new room
    /// </summary>
    /// <param name="room"></param>
    /// <returns></returns>
    public GameObject BuildRoom(GameObject room)
    {
        float width = GetRoomWidth(_latestRoom);
        Vector3 position = Vector3.right * width + _latestRoom.transform.position;
        GameObject newRoom = Instantiate(room, position, Quaternion.identity, this.transform);

        _latestRoom = newRoom;
        return newRoom;
    }

    public void ProgressToNextRoom()
    {
        GameObject thisRoom = _latestRoom;

        // kill all enemies inside the previous room
        foreach (GameObject enemy in GameManager.EnemySpawner.EnemySet)
            if (enemy != null)
                Destroy(enemy);

        // disconnect the old interactable
        GameManager.EnemySpawner.Interactable = null;

        // _lastRoom is changed
        BuildRoom();

        SetupRoom(_latestRoom);
        
        ((GridGraph) GameManager.AstarPath.graphs[0]).center.x += GetRoomWidth(thisRoom);
        GameManager.AstarPath.Scan();

    }

    /// <summary>
    /// Makes the given room the current room.
    /// Moves player to it, sets the spawner to spawn enemies in that room
    /// Sets the current objective to be the given room.
    /// </summary>
    /// <param name="room"></param>
    private void SetupRoom(GameObject room)
    {
        // move player spawn to next room
        MovePlayerToRoom(room);

        // change to the new room's SpawnPoints
        GameManager.EnemySpawner.SpawnPointsParent = GetSpawnEnemyPointsInRoom(room);

        // Note: could be null
        GameManager.EnemySpawner.Interactable = room.GetComponentInChildren<WaveReadyButton>();

        GameManager.EnemySpawner.InitializeSpawner();


        // subscribe to new room's Objective
        SetCurrentObjective(room);
    }

    private void SetCurrentObjective(GameObject room)
    {
        CurrentObjective = room.GetComponent<RoomObjective>();
        CurrentObjective.OnObjectiveComplete = ProgressToNextRoom;
    }


    private static float GetRoomWidth(GameObject room)
    {
        return room.GetComponentInChildren<CompositeCollider2D>().bounds.size.x;
    }

    private static void MovePlayerToRoom(GameObject room)
    {
        Vector3 playerSpawnPosition = GetPlayerSpawnInRoom(room);
        GameManager.Player.transform.position = playerSpawnPosition;
    }

    public static Vector3 GetPlayerSpawnInRoom(GameObject room)
    {
        return room.transform.Find("PlayerSpawn").position;
    }

    public static Transform GetSpawnEnemyPointsInRoom(GameObject room)
    {
        return room.transform.Find("SpawnPoints");
    }


    private void OnGUI()
    {
        if (GUI.Button(new Rect(x: 100, y: 20, width: 100, height: 20), "Build Room"))
            BuildRoom();

        if (GUI.Button(new Rect(x: 100, y: 40, width: 100, height: 20), "ProgressToNextRoom"))
            ProgressToNextRoom();

        if (GUI.Button(new Rect(x: 100, y: 60, width: 100, height: 20), "Build Room"))
            BuildRoom();
    }
}