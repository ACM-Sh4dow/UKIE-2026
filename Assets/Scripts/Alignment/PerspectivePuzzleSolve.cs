using System;
using UnityEngine;

public class PerspectivePuzzleSolve : MonoBehaviour
{
    #region Variables

    [Header("Puzzle Settings")]
    [SerializeField] private int solvingPlayerIndex;
    private ProtagonistController solvingPlayer;
    [SerializeField] private float distanceTolerance;
    [SerializeField] private float angleTolerance;
    
    [Header("Puzzle Pieces")]
    [SerializeField] private GameObject puzzleSolution;
    // [SerializeField] private GameObject puzzleResult;
    
    private bool isPuzzleSolved;

    #endregion
    
    #region Solving
    public void SolvePuzzle()
    {
        if (isPuzzleSolved) return;

        if ((Vector3.Distance(solvingPlayer.playerPosition, puzzleSolution.transform.position) <=
             distanceTolerance)
            && Quaternion.Dot(solvingPlayer.playerRotation.normalized, puzzleSolution.transform.rotation.normalized) <=
            angleTolerance)
        {
            // puzzleResult.SetActive(true);
            
            isPuzzleSolved = true;
            Debug.Log("Solved");
        }
    }
    #endregion
    #region Setting Puzzle

    private void Update()
    {
        if (solvingPlayer == null)
        {
            solvingPlayer = PlayerManager.players[solvingPlayerIndex];
        }

        if (solvingPlayer != null)
        {
            solvingPlayer.GivePuzzle(this);
        }
        
        SolvePuzzle();
    }

    #endregion
}