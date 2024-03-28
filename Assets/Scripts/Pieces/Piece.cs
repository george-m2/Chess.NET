using System.Collections.Generic;
using ChessNET;
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
        public bool hasMoved = false;

        private Vector3 desiredPosition; // position piece is moving towards
        private Vector3 desiredScale = Vector3.one;

        private void Update()
        {
            // smoothly move the piece towards the desired position and scale using vector interpolation
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10); 
            transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);

        }

        public void SetPosition(Vector3 position, bool force = false)
        {
            desiredPosition = position;
            if (force) // fallback: set the position immediately
                transform.position = desiredPosition;
        }

        public void SetScale(Vector3 scale, bool force = false)
        {
            desiredScale = scale;
            if (force) // fallback: set the scale immediately
                transform.localScale = desiredScale;
        }

        public virtual List<Vector2Int> GetAvailableMoves(ref Piece[,] board, int tileCountX, int tileCountY)
        {
            // list of pre-defined, fallback available moves
            // values are irrelevant, as they will be overwritten by child pieces
            List<Vector2Int> n = new() 
            {
                new Vector2Int(3, 3),
                new Vector2Int(3, 4),
                new Vector2Int(4, 3),
                new Vector2Int(4, 4)
            };

            return n;
        }

        public virtual SpecialMove GetSpecialMoves(ref Piece[,] board, ref List<Vector2Int[]> moveList,
            ref List<Vector2Int> availableMoves)
        {
            return SpecialMove.None; // no special moves by default, to be overridden by child pieces
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