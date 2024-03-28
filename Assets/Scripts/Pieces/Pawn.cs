using System.Collections.Generic;
using ChessNET;
using UnityEngine;

namespace Pieces
{
    public class Pawn : Piece
    {
        public override List<Vector2Int> GetAvailableMoves(ref Piece[,] board, int tileCountX, int tileCountY)
        {
            List<Vector2Int> n = new();
            int direction = (team == 0) ? 1 : -1; //up if white, down if black

            //one in front
            if (currentY + direction >= 0 && currentY + direction < tileCountY && board[currentX, currentY + direction] == null)
                n.Add(new Vector2Int(currentX, currentY + direction));

            //starting two in front
            if (currentY + direction >= 0 && currentY + direction < tileCountY && board[currentX, currentY + direction] == null)
            {
                switch (team)
                {
                    //white
                    case 0 when currentY == 1 && currentY + (direction * 2) < tileCountY && board[currentX, currentY + (direction * 2)] == null:
                    //black
                    case 1 when currentY == 6 && currentY + (direction * 2) >= 0 && board[currentX, currentY + (direction * 2)] == null:
                        n.Add(new Vector2Int(currentX, currentY + (direction * 2)));
                        break;
                }
            }

            //kill move
            if (currentX < tileCountX - 1 && currentY + direction >= 0 && currentY + direction < tileCountY)
                if (board[currentX + 1, currentY + direction] != null && //if there is a piece
                    board[currentX + 1, currentY + direction].team != team) //if piece is not on same team
                    n.Add(new Vector2Int(currentX + 1, currentY + direction)); //add to available moves
        
            if (currentX <= 0 || currentY + direction < 0 || currentY + direction >= tileCountY) return n; //if out of bounds, return
            if (board[currentX - 1, currentY + direction] != null && 
                board[currentX - 1, currentY + direction].team != team)
                n.Add(new Vector2Int(currentX - 1, currentY + direction));

            return n;
        }

        public override SpecialMove GetSpecialMoves(ref Piece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
        {
            int direction = (team == 0) ? 1 : -1; //up if white, down if black
        
            if ((team == 0 && currentY == 6) || (team == 1 && currentY == 1)) //if pawn is in final row - 1
                return SpecialMove.Promotion;
        
            //en passant
            if (moveList.Count <= 0) return SpecialMove.None;
            var lastMove = moveList[moveList.Count - 1]; //last move
            if (board[lastMove[1].x, lastMove[1].y].type != PieceType.Pawn) return SpecialMove.None; //if last move was pawn
            if (Mathf.Abs(lastMove[0].y - lastMove[1].y) != 2) return SpecialMove.None;
            if (board[lastMove[1].x, lastMove[1].y].team == team) return SpecialMove.None; //if pawn is not on same team
            if (lastMove[1].y != currentY) return SpecialMove.None; //if pawn is on same y
            if (lastMove[1].x == currentX + 1) //if pawn is on right
            {
                availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
                return SpecialMove.EnPassant;
            }

            if (lastMove[1].x == currentX - 1) //if pawn is on left
            {
                availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
                return SpecialMove.EnPassant;
            }
            return SpecialMove.None;
        }
    }
}



