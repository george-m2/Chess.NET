using System.Collections.Generic;
using Pieces;
using UnityEngine;

public class Pawn : Piece
{
    public override List<Vector2Int> GetAvailableMoves(ref Piece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> n = new();
        int direction = (team == 0) ? 1 : -1; //up if white, down if black

        //one in front
        if (board[currentX, currentY + direction] == null)
            n.Add(new Vector2Int(currentX, currentY + direction));

        //starting two in front

        if (board[currentX, currentY + direction] == null)
        {
            //white
            if (team == 0 && currentY == 1 && board[currentX, currentY + (direction * 2)] == null)
                n.Add(new Vector2Int(currentX, currentY + (direction * 2)));
            //black
            if (team == 1 && currentY == 6 && board[currentX, currentY + (direction * 2)] == null)
                n.Add(new Vector2Int(currentX, currentY + (direction * 2)));
        }

        //kill move
        if (currentX < tileCountX - 1)
            if (board[currentX + 1, currentY + direction] != null &&
                board[currentX + 1, currentY + direction].team != team)
                n.Add(new Vector2Int(currentX + 1, currentY + direction));
        if (currentX > 0)
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
        if (moveList.Count > 0)
        {
            var lastMove = moveList[moveList.Count - 1]; //last move
            if (board[lastMove[1].x, lastMove[1].y].type == PieceType.Pawn) //if last move was pawn
            {
                if (Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2)
                {
                    if (board[lastMove[1].x, lastMove[1].y].team != team) //if pawn is not on same team
                    {
                        if (lastMove[1].y == currentY) //if pawn is on same y
                        {
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
                        }
                    }
                }
            }
        }
        return SpecialMove.None;
    }
}



