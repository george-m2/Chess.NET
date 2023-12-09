using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChessNET;
using Pieces;
using UnityEngine;

public class PGNExporter : MonoBehaviour
{
    private Chessboard chessboard;
    private Chessboard.Move move;

    private void Awake()
    {
        chessboard = FindObjectOfType<Chessboard>();
    }

    private List<Vector2Int[]> CloneMoveList()
    {
        var chessboard = GetComponent<Chessboard>();
        chessboard = FindObjectOfType<Chessboard>();
        List<Vector2Int[]> clonedMoveList = chessboard.GetClonedMoveList();
        return clonedMoveList;
    }

    private string ConvertToPGN(Vector2Int position, Piece piece)
    {
        char file = (char)('a' + position.x);
        int rank = position.y + 1;

        string pieceNotation = piece.type switch
        {
            PieceType.Pawn => "",
            PieceType.Knight => "N",
            PieceType.Bishop => "B",
            PieceType.Rook => "R",
            PieceType.Queen => "Q",
            PieceType.King => "K",
            _ => ""
        };

        //was there a capture?
        string captureNotation = "";
        if (move.isCapture)
        {
            captureNotation = "x";
        }

        //was there a special move?
        switch (move.SpecialMoveType)
        {
            case SpecialMove.Castle:
                pieceNotation = move.EndPosition.x == 2 ? "O-O-O" : "O-O";
                break;

            case SpecialMove.Promotion:
            {
                var promotionPieceNotation = "";

                //stack is LIFO so we need to reverse it to get accurate FEN notation
                var reversedPromotedPieces = new Stack<Piece>(new List<Piece>(chessboard.promotedPieces).ToArray());
                if (reversedPromotedPieces.Count >
                    0) //initial promotion will have no promoted pieces, therefore we must check if the stack is empty
                {
                    var promotedPieceType = reversedPromotedPieces.Pop().type;

                    switch (promotedPieceType)
                    {
                        case PieceType.Queen:
                            promotionPieceNotation = "Q";
                            break;
                        case PieceType.Rook:
                            promotionPieceNotation = "R";
                            break;
                        case PieceType.Bishop:
                            promotionPieceNotation = "B";
                            break;
                        case PieceType.Knight:
                            promotionPieceNotation = "N";
                            break;
                    }

                    return $"{file}{rank}={promotionPieceNotation}";
                }

                break;
            }
            case SpecialMove.None:
                break;
        }

        return $"{pieceNotation}{captureNotation}{file}{rank}";
    }

    public string GeneratePGNString()
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

            // Iterate through each move in the array
            move = chessboard.moveHistory[i];
            foreach (var position in moveList[i])
            {
                var movingPiece = move.Piece;

                // Convert Vector2Int to PGN notation
                string pgnMove = ConvertToPGN(position, movingPiece);
                movesBuilder.Append(pgnMove + " ");
            }
        }

        pgnBuilder.AppendLine(movesBuilder.ToString().Trim());

        return pgnBuilder.ToString(); // Return the PGN string
    }

    public int ExportToPGN()
    {
        var pgn = GeneratePGNString();
        Debug.Log(pgn);
        System.IO.File.WriteAllText(@"pgn.txt", pgn);
        if (File.Exists(@"pgn.txt"))
        {
            return 0; // Success
        }

        return 1;
    }
}