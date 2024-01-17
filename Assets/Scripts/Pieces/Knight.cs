using System.Collections.Generic;
using Pieces;
using UnityEngine;

public class Knight : Piece
{
    public override List<Vector2Int> GetAvailableMoves(ref Piece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new();

        // L-shaped
        int[] dx = { 2, 1, -1, -2, -2, -1, 1, 2 };
        int[] dy = { 1, 2, 2, 1, -1, -2, -2, -1 };

        for (int i = 0; i < dx.Length; i++)
        {
            int newX = currentX + dx[i];
            int newY = currentY + dy[i];

            // Check if the new position is within the board bounds
            if (newX >= 0 && newX < tileCountX && newY >= 0 && newY < tileCountY)
            {
                // Check if the target position is empty or contains an opponent's piece
                if (board[newX, newY] == null || board[newX, newY].team != team)
                {
                    moves.Add(new Vector2Int(newX, newY));
                    Debug.Log(moves);
                }
            }
        }

        return moves;
    }
}