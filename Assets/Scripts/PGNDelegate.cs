using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static Chessboard;
public class PGNExporter : MonoBehaviour
{
    public List<Vector2Int[]> CloneMoveList()
    {
        Chessboard chessboard = GetComponent<Chessboard>();
        List<Vector2Int[]> clonedMoveList = chessboard.GetMoveList();
        return clonedMoveList;
    }
    
    public string ExportToPGN()
    {
        var moveList = CloneMoveList();
        var pgnBuilder = new StringBuilder();

        // PGN header
        pgnBuilder.AppendLine("[Event \"Unity Chess Game\"]");
        pgnBuilder.AppendLine("[Site \"Unity\"]");
        pgnBuilder.AppendLine("[Date \"" + System.DateTime.Now.ToString("yyyy.MM.dd") + "\"]");
        pgnBuilder.AppendLine("[Round \"1\"]");
        pgnBuilder.AppendLine("[White \"Player1\"]");
        pgnBuilder.AppendLine("[Black \"Player2\"]");
        pgnBuilder.AppendLine("[Result \"*\"]");

        // PGN moves
        StringBuilder movesBuilder = new StringBuilder();
        for (int i = 0; i < moveList.Count; i++)
        {
            if (i % 2 == 0)
            {
                movesBuilder.Append((i / 2) + 1 + ". ");
            }

            // Iterate through each Vector2Int in the array
            foreach (Vector2Int move in moveList[i])
            {
                // Convert Vector2Int to PGN notation
                string pgnMove = ConvertToPGN(move);
                movesBuilder.Append(pgnMove + " ");
            }
        }

        pgnBuilder.AppendLine(movesBuilder.ToString().Trim());

        // Save or print the PGN
        string pgn = pgnBuilder.ToString();
        System.IO.File.WriteAllText(@"pgn.txt", pgn);
        return pgn;
    }

    // Function to convert Vector2Int to PGN notation
    private string ConvertToPGN(Vector2Int position)
    {
        char file = (char)('a' + position.x);
        int rank = position.y + 1;
        return $"{file}{rank}";
    }
}
