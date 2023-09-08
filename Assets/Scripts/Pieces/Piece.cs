using System.Collections.Generic;
using UnityEngine;

namespace Pieces
{
    public enum PieceType
    {
        None = 0,
        Pawn = 1,
        Rook = 2,
        Knight = 3,
        Bishop = 4,
        Queen = 5,
        King = 6,
    }


    public class Piece : MonoBehaviour
    {
        public int team;
        public int currentX;
        public int currentY;
        public PieceType type;

        private Vector3 desiredPosition;
        private Vector3 desiredScale = Vector3.one;

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
            transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);

        }

        public virtual void SetPosition(Vector3 position, bool force = false)
        {
            desiredPosition = position;
            if (force)
                transform.position = desiredPosition;
        }

        public virtual void SetScale(Vector3 scale, bool force = false)
        {
            desiredScale = scale;
            if (force)
                transform.localScale = desiredScale;
        }

        public virtual List<Vector2Int> GetAvailableMoves(ref Piece[,] board, int tileCountX, int tileCountY)
        {
            List<Vector2Int> n = new()
            {
                new Vector2Int(3, 3),
                new Vector2Int(3, 4),
                new Vector2Int(4, 3),
                new Vector2Int(4, 4)
            };

            return n;
        }

        public virtual SpecialMove GetSpecialMoves(ref Piece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
        {
            return SpecialMove.None;
        }

        protected void CheckMovesInDirection(int dirX, int dirY, ref Piece[,] board, int tileCountX, int tileCountY, List<Vector2Int> moves)
        {
            int newX = currentX + dirX;
            int newY = currentY + dirY;

            while (newX >= 0 && newX < tileCountX && newY >= 0 && newY < tileCountY)
            {
                // Check if the target position is empty
                if (board[newX, newY] == null)
                {
                    moves.Add(new Vector2Int(newX, newY));
                }
                // Check if the target position contains an opponent's piece
                else if (board[newX, newY].team != team)
                {
                    moves.Add(new Vector2Int(newX, newY));
                    break; // Stop checking in this direction after encountering an opponent's piece
                }
                // Stop checking in this direction if there is a piece of the same team
                else if (board[newX, newY].team == team)
                {
                    break;
                }

                newX += dirX;
                newY += dirY;
            }
        }
    }
}