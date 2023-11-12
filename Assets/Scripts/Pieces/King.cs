using System.Collections.Generic;
using Pieces;
using UnityEngine;

public class King : Piece
{
    public override List<Vector2Int> GetAvailableMoves(ref Piece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new();

        // Define the possible king move offsets
        int[] offsetX = { 1, 1, 1, 0, 0, -1, -1, -1 };
        int[] offsetY = { 1, 0, -1, 1, -1, 1, 0, -1 };

        // Check each possible move offset
        for (int i = 0; i < offsetX.Length; i++)
        {
            int newX = currentX + offsetX[i];
            int newY = currentY + offsetY[i];

            // Check if the target position is within the board boundaries
            if (newX >= 0 && newX < tileCountX && newY >= 0 && newY < tileCountY)
            {
                // Check if the target position is empty or contains an opponent's piece
                if (board[newX, newY] == null || board[newX, newY].team != team)
                {
                    moves.Add(new Vector2Int(newX, newY));
                }
            }
        }

        return moves;
    }

    public override SpecialMove GetSpecialMoves(ref Piece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        var r = SpecialMove.None;

        var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7));
        var rightRook = moveList.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7));
        var leftRook = moveList.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7));


        if (kingMove == null && currentX == 4)
        {
            //black team
            if (team == 0)
            {
                if (leftRook == null) //has the rook moved yet?
                    if (board[0, 0].type == PieceType.Rook)
                        if (board[0, 0].team == 0)
                            if (board[3, 0] == null)
                                if (board[2, 0] == null)
                                    if (board[1, 0] == null)
                                    {
                                        availableMoves.Add(new Vector2Int(2, 0)); //-2x
                                        r = SpecialMove.Castle;
                                    }

                if (rightRook == null)
                    if (board[7, 0].type == PieceType.Rook)
                        if (board[7, 0].team == 0)
                            if (board[5, 0] == null)
                                if (board[6, 0] == null)
                                {
                                    availableMoves.Add(new Vector2Int(6, 0)); //move to 6x
                                    r = SpecialMove.Castle;
                                }
            }
            //white team
            else
            {
                if (rightRook == null) //has the rook moved yet?
                    if (board[0, 7].type == PieceType.Rook)
                        if (board[0, 7].team == 1)
                            if (board[3, 7] == null)
                                if (board[2, 7] == null)
                                    if (board[1, 7] == null)
                                    {
                                        availableMoves.Add(new Vector2Int(2, 7)); //-2x
                                        r = SpecialMove.Castle;
                                    }

                if (leftRook == null)
                    if (board[7, 7].type == PieceType.Rook)
                        if (board[7, 7].team == 1)
                            if (board[5, 7] == null)
                                if (board[6, 7] == null)
                                {
                                    availableMoves.Add(new Vector2Int(6, 7)); //move to 6x
                                    r = SpecialMove.Castle;
                                }
            }

        }

        return r;


    }
}

