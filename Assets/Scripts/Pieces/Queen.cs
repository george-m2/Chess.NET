using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public class Queen : Piece
    {
        public override List<Vector2Int> GetAvailableMoves(ref Piece[,] board, int tileCountX, int tileCountY)
        {
            List<Vector2Int> moves = new();

            // Check available moves in four diagonal directions: up-right, up-left, down-right, down-left
            CheckMovesInDirection(1, 1, ref board, tileCountX, tileCountY, moves);    // Up-Right
            CheckMovesInDirection(1, -1, ref board, tileCountX, tileCountY, moves);   // Up-Left
            CheckMovesInDirection(-1, 1, ref board, tileCountX, tileCountY, moves);   // Down-Right
            CheckMovesInDirection(-1, -1, ref board, tileCountX, tileCountY, moves);  // Down-Left

            // Check available moves in four straight directions: up, down, left, right
            CheckMovesInDirection(0, 1, ref board, tileCountX, tileCountY, moves);    // Up
            CheckMovesInDirection(0, -1, ref board, tileCountX, tileCountY, moves);   // Down
            CheckMovesInDirection(1, 0, ref board, tileCountX, tileCountY, moves);    // Right
            CheckMovesInDirection(-1, 0, ref board, tileCountX, tileCountY, moves);   // Left

            return moves;
        }
    }
}