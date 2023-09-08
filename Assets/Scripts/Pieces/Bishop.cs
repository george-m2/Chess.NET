using System.Collections.Generic;
using Pieces;
using UnityEngine;

public class Bishop : Piece
{
    public override List<Vector2Int> GetAvailableMoves(ref Piece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new();

        // Check available moves in four diagonal directions: up-right, up-left, down-right, down-left
        CheckMovesInDirection(1, 1, ref board, tileCountX, tileCountY, moves);    // Up-Right
        CheckMovesInDirection(1, -1, ref board, tileCountX, tileCountY, moves);   // Up-Left
        CheckMovesInDirection(-1, 1, ref board, tileCountX, tileCountY, moves);   // Down-Right
        CheckMovesInDirection(-1, -1, ref board, tileCountX, tileCountY, moves);  // Down-Left

        return moves;
    
    }
}