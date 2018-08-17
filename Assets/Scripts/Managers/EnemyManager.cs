using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ok, so we have levels of enemies and levels of attacks.
/// Depending on the difficulty, more high level enemies are allowed to spawn at the same time.
///
/// For room the player goes through, the Difficulty level increases, allowing for more difficult enemies to spawn,
/// and the attack limiter will
/// </summary>
public class EnemyManager : MonoBehaviour
{
	public int DifficultyLevel { get; private set; }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
