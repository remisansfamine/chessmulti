using UnityEngine;
using System.Collections.Generic;

/*
 * This class computes AI move decision
 * ComputeMove method is called from ChessGameMgr during AI update turn
 */

public class ChessAI : MonoBehaviour
{
    #region singleton
    static ChessAI instance = null;
    public static ChessAI Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<ChessAI>();
            return instance;
        }
    }
    #endregion

    #region AI

    public ChessGameMgr.Move ComputeMove()
    {
        // random AI move

        ChessGameMgr.Move move;
        move.From = 0;
        move.To = 1;

        List<ChessGameMgr.Move> moves = new List<ChessGameMgr.Move>(); ;
        ChessGameMgr.Instance.GetBoardState().GetValidMoves(ChessGameMgr.EChessTeam.Black, moves);

        if (moves.Count > 0)
            move = moves[Random.Range(0, moves.Count - 1)];

        return move;
    }

    #endregion

    #region monobehaviour
    void Start ()
    {
	
	}
	void Update ()
    {
	
	}

    #endregion
}
