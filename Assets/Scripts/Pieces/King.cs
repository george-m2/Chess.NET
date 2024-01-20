using System.Collections.Generic;
using ChessNET;
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

    public override SpecialMove GetSpecialMoves(ref Piece[,] board, ref List<Vector2Int[]> moveList,
        ref List<Vector2Int> availableMoves)
    {
        var r = SpecialMove.None;

        var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7));
        var leftRook = moveList.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7)); // Corrected position for left rook
        var rightRook = moveList.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7)); // Corrected position for right rook

        // Check if the king has not moved, regardless of its current position
        if (kingMove == null)
        {
            // black team
            if (team == 0)
            {
                // Check for left rook castling (queen side)
                if (leftRook == null && IsPathClear(0, currentY, board))
                {
                    availableMoves.Add(new Vector2Int(2, 0)); // Castling move for black team, queen side
                    r = SpecialMove.Castle;
                }

                // Check for right rook castling (king side)
                if (rightRook == null && IsPathClear(7, currentY, board))
                {
                    availableMoves.Add(new Vector2Int(6, 0)); // Castling move for black team, king side
                    r = SpecialMove.Castle;
                }
            }
            // white team
            else
            {
                // Check for left rook castling (queen side)
                if (leftRook == null && IsPathClear(0, 7, board))
                {
                    availableMoves.Add(new Vector2Int(2, 7)); // Castling move for white team, queen side
                    r = SpecialMove.Castle;
                }

                // Check for right rook castling (king side)
                if (rightRook == null && IsPathClear(7, 7, board))
                {
                    availableMoves.Add(new Vector2Int(6, 7)); // Castling move for white team, king side
                    r = SpecialMove.Castle;
                }
            }
        }

        return r;
    }

    // Helper method to check if the path between the king and rook is clear
    private bool IsPathClear(int rookX, int rookY, Piece[,] board)
    {
        int direction = rookX > currentX ? 1 : -1;
        for (int x = currentX + direction; x != rookX; x += direction)
        {
            if (board[x, rookY] != null)
                return false;
        }

        return true;
    }
}