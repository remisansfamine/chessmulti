using UnityEngine;
using System.Collections.Generic;

/*
 * The BoardState class stores the internal data values of the board
 * It holds a list of BoardSquare structs that contains info for each square : the type of piece (pawn, king, ... , none) and the team of the piece
 * It also contains methods to get valid moves for each type of piece accoring to the current board configuration
 * It can apply a selected move for a piece and eventually reset its values to default
 */

public partial class ChessGameMgr
{
    public struct BoardPos
    {
        public int X { get; set; }
        public int Y { get; set; }

        public BoardPos(int pos) { X = pos % BOARD_SIZE; Y = pos / BOARD_SIZE; }
        public BoardPos(int _x, int _y) { X = _x; Y = _y; }

        public static implicit operator int(BoardPos pos) { return pos.X + pos.Y * BOARD_SIZE; }

        static public int operator +(BoardPos pos1, BoardPos pos2)
        {
            int x = pos1.X + pos2.X;
            int y = pos1.Y + pos2.Y;

            return (x >= 0 && x < BOARD_SIZE && y >= 0 && y < BOARD_SIZE) ? new BoardPos(x, y) : -1;
        }

        public int GetRight()
        {
            return (X == BOARD_SIZE - 1) ? -1 : new BoardPos(X + 1, Y);
        }

        public int GetLeft()
        {
            return (X == 0) ? -1 : new BoardPos(X - 1, Y);
        }

        public int GetTop()
        {
            return (Y == BOARD_SIZE - 1) ? -1 : new BoardPos(X, Y + 1);
        }

        public int GetBottom()
        {
            return (Y == 0) ? -1 : new BoardPos(X, Y - 1);
        }
    }

    public class BoardState
    {
        public enum EMoveResult
        {
            Normal,
            Promotion,
            Castling_Long,
            Castling_Short
        }

        public List<BoardSquare> Squares = null;

        private bool isWhiteCastlingDone = false;
        private bool isBlackCastlingDone = false;

        public bool IsValidSquare(int pos, EChessTeam team, int teamFlag)
        {
            if (pos < 0)
                return false;

            bool isTeamValid = ((Squares[pos].Team == EChessTeam.None) && ((teamFlag & (int)ETeamFlag.None) > 0)) ||
                ((Squares[pos].Team != team && Squares[pos].Team != EChessTeam.None) && ((teamFlag & (int)ETeamFlag.Enemy) > 0));

            return isTeamValid;
        }

        public void AddMoveIfValidSquare(EChessTeam team, int from, int to, List<Move> moves, int teamFlag = (int)ETeamFlag.Enemy | (int)ETeamFlag.None)
        {
            if (IsValidSquare(to, team, teamFlag))
            {
                Move move;
                move.From = from;
                move.To = to;
                moves.Add(move);
            }
        }

        public void GetValidKingMoves(EChessTeam team, int pos, List<Move> moves)
        {
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(1, 0)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(1, 1)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(0, 1)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(-1, 1)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(-1, 0)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(-1, -1)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(0, -1)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(1, -1)), moves);
        }

        public void GetValidQueenMoves(EChessTeam team, int pos, List<Move> moves)
        {
            GetValidRookMoves(team, pos, moves);
            GetValidBishopMoves(team, pos, moves);
        }

        public void GetValidPawnMoves(EChessTeam team, int pos, List<Move> moves)
        {
            int FrontPos = -1, LeftFrontPos = -1, RightFrontPos = -1;
            if (team == EChessTeam.White)
            {
                FrontPos = new BoardPos(pos).GetTop();
                if (FrontPos != -1)
                {
                    LeftFrontPos = new BoardPos(FrontPos).GetLeft();
                    RightFrontPos = new BoardPos(FrontPos).GetRight();
                }
                if ( new BoardPos(pos).Y == 1 && Squares[pos + BOARD_SIZE].Piece == EPieceType.None)
                {
                    AddMoveIfValidSquare(team, pos, new BoardPos(FrontPos).GetTop(), moves, (int)ETeamFlag.None);
                }
            }
            else
            {
                FrontPos = new BoardPos(pos).GetBottom();
                if (FrontPos != -1)
                {
                    RightFrontPos = new BoardPos(FrontPos).GetLeft();
                    LeftFrontPos = new BoardPos(FrontPos).GetRight();
                }

                if (new BoardPos(pos).Y == 6 && Squares[pos - BOARD_SIZE].Piece == EPieceType.None)
                {
                    AddMoveIfValidSquare(team, pos, new BoardPos(FrontPos).GetBottom(), moves, (int)ETeamFlag.None);
                }
            }

            AddMoveIfValidSquare(team, pos, FrontPos, moves, (int)ETeamFlag.None);
            AddMoveIfValidSquare(team, pos, LeftFrontPos, moves, (int)ETeamFlag.Enemy);
            AddMoveIfValidSquare(team, pos, RightFrontPos, moves, (int)ETeamFlag.Enemy);
        }

        public void GetValidRookMoves(EChessTeam team, int pos, List<Move> moves)
        {
            bool doBreak = false;
            int TopPos = new BoardPos(pos).GetTop();
            while (!doBreak && TopPos >= 0 && Squares[TopPos].Team != team)
            {
                AddMoveIfValidSquare(team, pos, TopPos, moves);
                doBreak = Squares[TopPos].Team != EChessTeam.None;
                TopPos = new BoardPos(TopPos).GetTop();
            }

            doBreak = false;
            int BottomPos = new BoardPos(pos).GetBottom();
            while (!doBreak && BottomPos >= 0 && Squares[BottomPos].Team != team)
            {
                AddMoveIfValidSquare(team, pos, BottomPos, moves);
                doBreak = Squares[BottomPos].Team != EChessTeam.None;
                BottomPos = new BoardPos(BottomPos).GetBottom();
            }

            doBreak = false;
            int LeftPos = new BoardPos(pos).GetLeft();
            while (!doBreak && LeftPos >= 0 && Squares[LeftPos].Team != team)
            {
                AddMoveIfValidSquare(team, pos, LeftPos, moves);
                doBreak = Squares[LeftPos].Team != EChessTeam.None;
                LeftPos = new BoardPos(LeftPos).GetLeft();
            }

            doBreak = false;
            int RightPos = new BoardPos(pos).GetRight();
            while (!doBreak && RightPos >= 0 && Squares[RightPos].Team != team)
            {
                AddMoveIfValidSquare(team, pos, RightPos, moves);
                doBreak = Squares[RightPos].Team != EChessTeam.None;
                RightPos = new BoardPos(RightPos).GetRight();
            }
        }

        public void GetValidBishopMoves(EChessTeam team, int pos, List<Move> moves)
        {
            bool doBreak = false;
            int TopRightPos = new BoardPos(pos) + new BoardPos(1, 1);
            while (!doBreak && TopRightPos >= 0 && Squares[TopRightPos].Team != team)
            {

                AddMoveIfValidSquare(team, pos, TopRightPos, moves);
                doBreak = Squares[TopRightPos].Team != EChessTeam.None;
                TopRightPos = new BoardPos(TopRightPos) + new BoardPos(1, 1);
            }

            doBreak = false;
            int TopLeftPos = new BoardPos(pos) + new BoardPos(-1, 1);
            while (!doBreak && TopLeftPos >= 0 && Squares[TopLeftPos].Team != team)
            {

                AddMoveIfValidSquare(team, pos, TopLeftPos, moves);
                doBreak = Squares[TopLeftPos].Team != EChessTeam.None;
                TopLeftPos = new BoardPos(TopLeftPos) + new BoardPos(-1, 1);
            }

            doBreak = false;
            int BottomRightPos = new BoardPos(pos) + new BoardPos(1, -1);
            while (!doBreak && BottomRightPos >= 0 && Squares[BottomRightPos].Team != team)
            {

                AddMoveIfValidSquare(team, pos, BottomRightPos, moves);
                doBreak = Squares[BottomRightPos].Team != EChessTeam.None;
                BottomRightPos = new BoardPos(BottomRightPos) + new BoardPos(1, -1);
            }

            doBreak = false;
            int BottomLeftPos = new BoardPos(pos) + new BoardPos(-1, -1);
            while (!doBreak && BottomLeftPos >= 0 && Squares[BottomLeftPos].Team != team)
            {

                AddMoveIfValidSquare(team, pos, BottomLeftPos, moves);
                doBreak = Squares[BottomLeftPos].Team != EChessTeam.None;
                BottomLeftPos = new BoardPos(BottomLeftPos) + new BoardPos(-1, -1);
            }
        }

        public void GetValidKnightMoves(EChessTeam team, int pos, List<Move> moves)
        {
            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(1, 2), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(2, 1), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(-1, 2), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(-2, 1), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(1, -2), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(2, -1), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(-1, -2), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(-2, -1), moves);
        }

        public void GetValidMoves(EChessTeam team, List<Move> moves)
        {
            for (int i = 0; i < BOARD_SIZE * BOARD_SIZE; ++i)
            {
                if (Squares[i].Team == team)
                {
                    switch (Squares[i].Piece)
                    {
                        case EPieceType.King: GetValidKingMoves(team, i, moves); break;
                        case EPieceType.Queen: GetValidQueenMoves(team, i, moves); break;
                        case EPieceType.Pawn: GetValidPawnMoves(team, i, moves); break;
                        case EPieceType.Rook: GetValidRookMoves(team, i, moves); break;
                        case EPieceType.Bishop: GetValidBishopMoves(team, i, moves); break;
                        case EPieceType.Knight: GetValidKnightMoves(team, i, moves); break;
                        default: break;
                    }
                }
            }
        }

        public bool IsValidMove(EChessTeam team, Move move)
        {
            List<Move> validMoves = new List<Move>();
            GetValidMoves(team, validMoves);

            return validMoves.Contains(move);
        }

        // returns move result if a special move occured (pawn promotion, castling...)
        public EMoveResult PlayUnsafeMove(Move move)
        {
            Squares[move.To] = Squares[move.From];

            BoardSquare square = Squares[move.From];
            square.Piece = EPieceType.None;
            square.Team = EChessTeam.None;
            Squares[move.From] = square;

            if (CanPromotePawn(move))
            {
                // promote pawn to queen
                BoardSquare destSquare = Squares[move.To];
                SetPieceAtSquare(move.To, destSquare.Team, EPieceType.Queen);
                return EMoveResult.Promotion;
            }
            // Castling move
            return ComputeCastling(move);
        }

        private bool CanPromotePawn(Move move)
        {
            BoardSquare destSquare = Squares[move.To];
            if (destSquare.Piece == EPieceType.Pawn)
            {
                BoardPos pos = new BoardPos(move.To);
                if (destSquare.Team == EChessTeam.Black && pos.Y == 0 || destSquare.Team == EChessTeam.White && pos.Y == (BOARD_SIZE - 1))
                    return true;
            }
            return false;
        }

        // compute castling move if applicable
        private EMoveResult ComputeCastling(Move move)
        {
            BoardSquare destSquare = Squares[move.To];

            if ((destSquare.Team == EChessTeam.White && isWhiteCastlingDone)
             || (destSquare.Team == EChessTeam.Black && isBlackCastlingDone))
                return EMoveResult.Normal;

            // rook piece
            if (destSquare.Piece == EPieceType.Rook)
            {
                // short castling case
                if ((destSquare.Team == EChessTeam.White && move.From == (BOARD_SIZE - 1) && move.To == 5) // white line
                 || (destSquare.Team == EChessTeam.Black && move.From == (Squares.Count - 1) && move.To == Squares.Count - 3)) // black line
                {
                    if (TryExecuteCastling(move.To, true))
                        return EMoveResult.Castling_Short;
                }
                // long castling case
                if ((destSquare.Team == EChessTeam.White && move.From == 0 && move.To == 3) // white line
                || (destSquare.Team == EChessTeam.Black && move.From == (Squares.Count - 8) && move.To == Squares.Count - 5)) // black line
                {
                    if (TryExecuteCastling(move.To, false))
                        return EMoveResult.Castling_Long;
                }
            }

            return EMoveResult.Normal;
        }

        private bool TryExecuteCastling(int moveToIndex, bool isShortCastling)
        {
            int kingSquareIndex = isShortCastling ? (moveToIndex - 1) : moveToIndex + 1;
            int kingFinalSquareIndex = isShortCastling ? (moveToIndex + 1) : moveToIndex - 1;
            BoardSquare destSquare = Squares[moveToIndex];
            BoardSquare kingSquare = Squares[kingSquareIndex];
            if (kingSquare.Piece == EPieceType.King && kingSquare.Team == destSquare.Team)
            {
                BoardSquare tempSquare = kingSquare; // king square to be moved
                Squares[kingSquareIndex] = BoardSquare.Empty(); // replace by empty square
                Squares[kingFinalSquareIndex] = tempSquare;

                if (destSquare.Team == EChessTeam.White)
                    isWhiteCastlingDone = true;
                else
                    isBlackCastlingDone = true;

                return true;
            }

            return false;
        }

        // approximation : opponent king must be "eaten" to win instead of detecting checkmate state
        public bool DoesTeamLose(EChessTeam team)
        {
            for (int i = 0; i < Squares.Count; ++i)
            {
                if (Squares[i].Team == team && Squares[i].Piece == EPieceType.King)
                {
                    return false;
                }
            }
            return true;
        }

        private void SetPieceAtSquare(int index, EChessTeam team, EPieceType piece)
        {
            if (index > Squares.Count)
                return;
            BoardSquare square = Squares[index];
            square.Piece = piece;
            square.Team = team;
            Squares[index] = square;
        }

        public void Reset()
        {
            isWhiteCastlingDone = false;
            isBlackCastlingDone = false;

            if (Squares == null)
            {
                Squares = new List<BoardSquare>();

                // init squares
                for (int i = 0; i < BOARD_SIZE * BOARD_SIZE; i++)
                {
                    BoardSquare square = new BoardSquare();
                    square.Piece = EPieceType.None;
                    square.Team = EChessTeam.None;
                    Squares.Add(square);
                }
            }
            else
            {
                for (int i = 0; i < Squares.Count; ++i)
                {
                    SetPieceAtSquare(i, EChessTeam.None, EPieceType.None);
                }
            }

             // White
            for (int i = BOARD_SIZE; i < BOARD_SIZE*2; ++i)
            {
                SetPieceAtSquare(i, EChessTeam.White, EPieceType.Pawn);
            }
            SetPieceAtSquare(0, EChessTeam.White, EPieceType.Rook);
            SetPieceAtSquare(1, EChessTeam.White, EPieceType.Knight);
            SetPieceAtSquare(2, EChessTeam.White, EPieceType.Bishop);
            SetPieceAtSquare(3, EChessTeam.White, EPieceType.Queen);
            SetPieceAtSquare(4, EChessTeam.White, EPieceType.King);
            SetPieceAtSquare(5, EChessTeam.White, EPieceType.Bishop);
            SetPieceAtSquare(6, EChessTeam.White, EPieceType.Knight);
            SetPieceAtSquare(7, EChessTeam.White, EPieceType.Rook);

            // Black
            for (int i = BOARD_SIZE * (BOARD_SIZE - 2) ; i < BOARD_SIZE * (BOARD_SIZE - 1); ++i)
            {
                SetPieceAtSquare(i, EChessTeam.Black, EPieceType.Pawn);
            }
            int startIndex = BOARD_SIZE * (BOARD_SIZE - 1);
            SetPieceAtSquare(startIndex, EChessTeam.Black, EPieceType.Rook);
            SetPieceAtSquare(startIndex + 1, EChessTeam.Black, EPieceType.Knight);
            SetPieceAtSquare(startIndex + 2, EChessTeam.Black, EPieceType.Bishop);
            SetPieceAtSquare(startIndex + 3, EChessTeam.Black, EPieceType.Queen);
            SetPieceAtSquare(startIndex + 4, EChessTeam.Black, EPieceType.King);
            SetPieceAtSquare(startIndex + 5, EChessTeam.Black, EPieceType.Bishop);
            SetPieceAtSquare(startIndex + 6, EChessTeam.Black, EPieceType.Knight);
            SetPieceAtSquare(startIndex + 7, EChessTeam.Black, EPieceType.Rook);
        }
    }
}

