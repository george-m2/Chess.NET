using System.Collections.Generic;
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
            if (board[currentX + 1, currentY + direction] != null && board[currentX + 1, currentY + direction].team != team)
                n.Add(new Vector2Int(currentX + 1, currentY + direction));
        if (currentX > 0)
            if (board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
                n.Add(new Vector2Int(currentX - 1, currentY + direction));

        return n;
    }
}
