using System;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

/*
 * This singleton manages the whole chess game
 *  - board data (see BoardState class)
 *  - piece models instantiation
 *  - player interactions (piece grab, drag and release)
 *  - AI update calls (see UpdateAITurn and ChessAI class)
 */

public partial class ChessGameMgr : MonoBehaviour
{

    #region singleton
    static ChessGameMgr instance = null;
    public static ChessGameMgr Instance {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<ChessGameMgr>();
            return instance;
        }
    }
    #endregion

    [SerializeField]
    private bool IsAIEnabled = true;

    private ChessAI chessAI = null;
    private Transform boardTransform = null;
    private static int BOARD_SIZE = 8;
    private int pieceLayerMask;
    private int boardLayerMask;
    private bool isPlaying = false;

    [SerializeField] private GameObject endScreen = null;
    [SerializeField] private TMP_Text winField = null;

    #region enums
    public enum EPieceType : uint
    {
        Pawn = 0,
        King,
        Queen,
        Rook,
        Knight,
        Bishop,
        NbPieces,
        None
    }

    public enum EChessTeam
    {
        White = 0,
        Black,
        None
    }

    public enum ETeamFlag : uint
    {
        None = 1 << 0,
        Friend = 1 << 1,
        Enemy = 1 << 2
    }
    #endregion

    #region structs and classes
    public struct BoardSquare
    {
        public EPieceType Piece;
        public EChessTeam Team;

        public BoardSquare(EPieceType p, EChessTeam t)
        {
            Piece = p;
            Team = t;
        }

        static public BoardSquare Empty()
        {
            BoardSquare res;
            res.Piece = EPieceType.None;
            res.Team = EChessTeam.None;
            return res;
        }
    }

    [Serializable]
    public struct Move
    {
        public int From;
        public int To;

        public override bool Equals(object o)
        {
            try
            {
                return (bool)(this == (Move)o);
            }
            catch
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return From + To;
        }

        public static bool operator ==(Move move1, Move move2)
        {
            return move1.From == move2.From && move1.To == move2.To;
        }

        public static bool operator !=(Move move1, Move move2)
        {
            return move1.From != move2.From || move1.To != move2.To;
        }
    }

    #endregion

    #region networking
    [SerializeField] private Player m_player = null;

    private void OnClientDisconnection()
    {
        Debug.Log("Client disconnected, AI is now activated");
        IsAIEnabled = true;
    }

    #endregion

    #region chess game method

    BoardState boardState = null;
    public BoardState GetBoardState() { return boardState; }

    public EChessTeam team;
    public EChessTeam teamTurn = EChessTeam.None;

    public Move? currentMove = null;

    List<uint> scores;

    public delegate void PlayerTurnEvent(bool isWhiteMove);
    public event PlayerTurnEvent OnPlayerTurn = null;

    public delegate void ScoreUpdateEvent(uint whiteScore, uint blackScore);
    public event ScoreUpdateEvent OnScoreUpdated = null;


    public void EnableAI(bool value)
    {
        IsAIEnabled = value;
    }

    public void PrepareGame(bool resetScore = true)
    {
        chessAI = ChessAI.Instance;

        // Start game
        boardState.Reset();

        teamTurn = EChessTeam.White;
        if (scores == null)
        {
            scores = new List<uint>();
            scores.Add(0);
            scores.Add(0);
        }
        if (resetScore)
        {
            scores.Clear();
            scores.Add(0);
            scores.Add(0);
        }
    }

    public void CheckMove(Move move)
    {
        bool isValid = boardState.IsValidMove(teamTurn, move);
        m_player.networkUser.SendPacket(EPacketType.MOVE_VALIDITY, isValid);

        if (!isValid) return;

        UpdateTurn(move);

        m_player.networkUser.SendPacket(EPacketType.TEAM_TURN, teamTurn);

        m_player.networkUser.SendPacket(EPacketType.SPECTATORS_MOVEMENTS, move);

        // SEND TO SPECS THE CORRECT MOVE
    }

    public bool TryMove(Move move)
    {
        bool isValid = boardState.IsValidMove(teamTurn, move);

        if (!isValid)
            return false;

        m_player.networkUser.SendPacket(EPacketType.MOVEMENTS, move);

        UpdateTurn(move);

        m_player.networkUser.SendPacket(EPacketType.TEAM_TURN, teamTurn);

        return true;
    }

    public void PlayTurn(Move move)
    {
        if (m_player.isHost)
        {
            if (!TryMove(move))
                UpdatePieces();
        }
        else
        {
            m_player.networkUser.SendPacket(EPacketType.MOVEMENTS, move);
        }
    }

    public void ResetMove()
    {
        currentMove = null;
        UpdatePieces();
    }

    public void UpdateTurn()
    {
        if (currentMove.HasValue)
        {
            UpdateTurn(currentMove.Value);
            currentMove = null;
        }
    }

    private void EndGame()
    {
        // increase score and reset board
        scores[(int)teamTurn]++;
        if (OnScoreUpdated != null)
            OnScoreUpdated(scores[0], scores[1]);

        StopGame();

        if (endScreen)
        {
            endScreen.SetActive(true);
            winField.text = $"{teamTurn.ToString()} team won !";
        }
    }

    public void ResetBoard()
    {
        PrepareGame(false);
        // remove extra piece instances if pawn promotions occured
        teamPiecesArray[0].ClearPromotedPieces();
        teamPiecesArray[1].ClearPromotedPieces();

        UpdatePieces();
    }

    public void ResetGame()
    {
        teamTurn = EChessTeam.White;

        m_player.networkUser.SendNetMessage("ResetGame");

        isPlaying = true;

        endScreen?.SetActive(false);

        ResetBoard();
    }

    public void UpdateTurn(Move move)
    {
        BoardState.EMoveResult result = boardState.PlayUnsafeMove(move);
        if (result == BoardState.EMoveResult.Promotion)
        {
            // instantiate promoted queen gameobject
            AddQueenAtPos(move.To);
        }

        EChessTeam otherTeam = (teamTurn == EChessTeam.White) ? EChessTeam.Black : EChessTeam.White;

        if (boardState.DoesTeamLose(otherTeam))
        {
            EndGame();
        }
        else
        {
            teamTurn = otherTeam;
        }
        // raise event
        if (OnPlayerTurn != null)
            OnPlayerTurn(teamTurn == EChessTeam.White);

        UpdatePieces();
    }

    // used to instantiate newly promoted queen
    private void AddQueenAtPos(int pos)
    {
        teamPiecesArray[(int)teamTurn].AddPiece(EPieceType.Queen);
        GameObject[] crtTeamPrefabs = (teamTurn == EChessTeam.White) ? WhitePiecesPrefab : BlackPiecesPrefab;
        GameObject crtPiece = Instantiate(crtTeamPrefabs[(uint)EPieceType.Queen]);
        teamPiecesArray[(int)teamTurn].StorePiece(crtPiece, EPieceType.Queen);
        crtPiece.transform.position = GetWorldPos(pos);
    }

    public bool IsPlayerTurn()
    {
        return teamTurn == EChessTeam.White;
    }

    public BoardSquare GetSquare(int pos)
    {
        return boardState.Squares[pos];
    }

    public uint GetScore(EChessTeam team)
    {
        return scores[(int)team];
    }

    private void UpdateBoardPiece(Transform pieceTransform, int destPos)
    {
        pieceTransform.position = GetWorldPos(destPos);
    }

    private Vector3 GetWorldPos(int pos)
    {
        Vector3 piecePos = boardTransform.position;
        piecePos.y += zOffset;
        piecePos.x = -widthOffset + pos % BOARD_SIZE;
        piecePos.z = -widthOffset + pos / BOARD_SIZE;

        return piecePos;
    }

    private int GetBoardPos(Vector3 worldPos)
    {
        int xPos = Mathf.FloorToInt(worldPos.x + widthOffset) % BOARD_SIZE;
        int zPos = Mathf.FloorToInt(worldPos.z + widthOffset);

        return xPos + zPos * BOARD_SIZE;
    }

    public void StopGame()
    {
        isPlaying = false;
    }

    public void StartGame()
    {
        teamTurn = EChessTeam.White;
        isPlaying = true;
    }

    #endregion

    #region MonoBehaviour

    private TeamPieces[] teamPiecesArray = new TeamPieces[2];
    private float zOffset = 0.5f;
    private float widthOffset = 3.5f;

    void Start()
    {
        pieceLayerMask = 1 << LayerMask.NameToLayer("Piece");
        boardLayerMask = 1 << LayerMask.NameToLayer("Board");

        boardTransform = GameObject.FindGameObjectWithTag("Board").transform;

        LoadPiecesPrefab();

        boardState = new BoardState();

        PrepareGame();

        teamPiecesArray[0] = null;
        teamPiecesArray[1] = null;

        CreatePieces();

        if (OnPlayerTurn != null)
            OnPlayerTurn(teamTurn == EChessTeam.White);
        if (OnScoreUpdated != null)
            OnScoreUpdated(scores[0], scores[1]);
    }

    void Update()
    {
        if (!isPlaying) return;

        // human player always plays white
        if (teamTurn == team)
            UpdatePlayerTurn();
        // AI plays black
        else if (IsAIEnabled)
            UpdateAITurn();
    }
    #endregion

    #region pieces

    GameObject[] WhitePiecesPrefab = new GameObject[6];
    GameObject[] BlackPiecesPrefab = new GameObject[6];

    void LoadPiecesPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhitePawn");
        WhitePiecesPrefab[(uint)EPieceType.Pawn] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteKing");
        WhitePiecesPrefab[(uint)EPieceType.King] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteQueen");
        WhitePiecesPrefab[(uint)EPieceType.Queen] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteRook");
        WhitePiecesPrefab[(uint)EPieceType.Rook] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteKnight");
        WhitePiecesPrefab[(uint)EPieceType.Knight] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteBishop");
        WhitePiecesPrefab[(uint)EPieceType.Bishop] = prefab;

        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackPawn");
        BlackPiecesPrefab[(uint)EPieceType.Pawn] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackKing");
        BlackPiecesPrefab[(uint)EPieceType.King] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackQueen");
        BlackPiecesPrefab[(uint)EPieceType.Queen] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackRook");
        BlackPiecesPrefab[(uint)EPieceType.Rook] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackKnight");
        BlackPiecesPrefab[(uint)EPieceType.Knight] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackBishop");
        BlackPiecesPrefab[(uint)EPieceType.Bishop] = prefab;
    }

    void CreatePieces()
    {
        // Instantiate all pieces according to board data
        if (teamPiecesArray[0] == null)
            teamPiecesArray[0] = new TeamPieces();
        if (teamPiecesArray[1] == null)
            teamPiecesArray[1] = new TeamPieces();

        GameObject[] crtTeamPrefabs = null;
        int crtPos = 0;
        foreach (BoardSquare square in boardState.Squares)
        {
            crtTeamPrefabs = (square.Team == EChessTeam.White) ? WhitePiecesPrefab : BlackPiecesPrefab;
            if (square.Piece != EPieceType.None)
            {
                GameObject crtPiece = Instantiate(crtTeamPrefabs[(uint)square.Piece]);
                teamPiecesArray[(int)square.Team].StorePiece(crtPiece, square.Piece);

                // set position
                Vector3 piecePos = boardTransform.position;
                piecePos.y += zOffset;
                piecePos.x = -widthOffset + crtPos % BOARD_SIZE;
                piecePos.z = -widthOffset + crtPos / BOARD_SIZE;
                crtPiece.transform.position = piecePos;
            }
            crtPos++;
        }
    }

    void UpdatePieces()
    {
        teamPiecesArray[0].Hide();
        teamPiecesArray[1].Hide();

        for (int i = 0; i < boardState.Squares.Count; i++)
        {
            BoardSquare square = boardState.Squares[i];
            if (square.Team == EChessTeam.None)
                continue;

            int teamId = (int)square.Team;
            EPieceType pieceType = square.Piece;

            teamPiecesArray[teamId].SetPieceAtPos(pieceType, GetWorldPos(i));
        }
    }

    #endregion

    #region gameplay

    Transform grabbed = null;
    float maxDistance = 100f;
    int startPos = 0;
    int destPos = 0;

    void UpdateAITurn()
    {
        Move move = chessAI.ComputeMove();
        PlayTurn(move);

        UpdatePieces();
    }

    void UpdatePlayerTurn()
    {
        if (Input.GetMouseButton(0))
        {
            if (grabbed)
                ComputeDrag();
            else
                ComputeGrab();
        }
        else if (grabbed != null)
        {
            // find matching square when releasing grabbed piece

            if (CameraRayCast(out RaycastHit hit, maxDistance, boardLayerMask))
            {
                grabbed.root.position = hit.transform.position + Vector3.up * zOffset;
            }

            destPos = GetBoardPos(grabbed.root.position);
            if (startPos != destPos)
            {
                Move move = new Move();
                move.From = startPos;
                move.To = destPos;

                currentMove = move;

                PlayTurn(move);
            }
            else
            {
                grabbed.root.position = GetWorldPos(startPos);
            }
            grabbed = null;
        }
    }

    void ComputeDrag()
    {
        if (CameraRayCast(out RaycastHit hit, maxDistance, boardLayerMask))
        {
            grabbed.root.position = hit.point;
        }
    }

    void ComputeGrab()
    {
        // grab a new chess piece from board
        if (CameraRayCast(out RaycastHit hit, maxDistance, pieceLayerMask))
        {
            grabbed = hit.transform;
            startPos = GetBoardPos(hit.transform.position);
        }
    }

    bool CameraRayCast(out RaycastHit hit, float maxDist, int layerMask)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit, maxDist, layerMask);
    }

    #endregion
}
