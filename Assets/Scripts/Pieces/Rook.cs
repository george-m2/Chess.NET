using System.Collections.Generic;
using UnityEngine;

public class Rook : Piece
{
    public override List<Vector2Int> GetAvailableMoves(ref Piece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new();

        // Check available moves in four directions: up, down, left, right
        CheckMovesInDirection(0, 1, ref board, tileCountX, tileCountY, moves);  // Up
        CheckMovesInDirection(0, -1, ref board, tileCountX, tileCountY, moves); // Down
        CheckMovesInDirection(1, 0, ref board, tileCountX, tileCountY, moves);  // Right
        CheckMovesInDirection(-1, 0, ref board, tileCountX, tileCountY, moves); // Left

        return moves;
    }
}