using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PGNExporter : MonoBehaviour
{
private Chessboard chessboard;
    private List<Vector2Int[]> CloneMoveList()
    {
        var chessboard = GetComponent<Chessboard>();
        chessboard = FindObjectOfType<Chessboard>();
        List<Vector2Int[]> clonedMoveList = chessboard.GetClonedMoveList();
        return clonedMoveList;
    }

    private string ConvertToPGN(Vector2Int position)
    {
        char file = (char)('a' + position.x);
        int rank = position.y + 1;
        return $"{file}{rank}";
    }
    
    public void ExportToPGN()
    {
        var moveList = CloneMoveList();
        var gamesPlayed = Chessboard.NoOfGamesPlayedInSession;
        var winner = Chessboard.Winner;
        var pgnBuilder = new StringBuilder();
        
        // PGN header
        pgnBuilder.AppendLine("[Event \"Chess.NET\"]");
        pgnBuilder.AppendLine("[Site \"" + SystemInfo.deviceName + "\"]");
        pgnBuilder.AppendLine("[Date \"" + System.DateTime.Now.ToString("yyyy.MM.dd") + "\"]");
        pgnBuilder.AppendLine("[Round \"" + gamesPlayed + "\"]");
        pgnBuilder.AppendLine("[White \"Player1\"]");
        pgnBuilder.AppendLine("[Black \"Player2\"]");
        pgnBuilder.AppendLine("[Result \"" + winner + "\"]");

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

        var pgn = pgnBuilder.ToString();
        Debug.Log(pgn);
        System.IO.File.WriteAllText(@"pgn.txt", pgn);
    }
    
}