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
}