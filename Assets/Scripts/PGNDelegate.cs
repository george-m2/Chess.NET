using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ChessNET;
using Pieces;
using UnityEngine;

namespace PGNDelegate
{
    public class PGNExporter : MonoBehaviour
    {
        private Chessboard _chessboard;
        private Chessboard.Move _move;

        private void Awake()
        {
            _chessboard = FindObjectOfType<Chessboard>();
        }

        private List<Vector2Int[]> CloneMoveList()
        {
            return _chessboard?.GetClonedMoveList() ?? new List<Vector2Int[]>();
        }

        private string ConvertToPGN(Vector2Int position, PieceType piece)
        {
            char file = (char)('h' - position.x); 
            int rank = 8 - position.y;

            string pieceNotation = GetPieceNotation(piece);
            string moveNotation = $"{file}{rank}";

            if (_move.isCapture)
            {
                if (piece == PieceType.Pawn)
                {
                    // For pawn captures, prepend the file of departure
                    char departureFile = (char)('h' - _move.StartPosition.x);
                    moveNotation = $"{departureFile}x{moveNotation}";
                }
                else
                {
                    // For other pieces, simply prepend 'x' to the destination square
                    moveNotation = $"{pieceNotation}x{moveNotation}";
                }
            }
            else
            {
                moveNotation = $"{pieceNotation}{moveNotation}";
            }

            string specialMoveNotation = GetSpecialMoveNotation();

            return $"{moveNotation}{specialMoveNotation}";
        }



        private string GetPieceNotation(PieceType piece) => piece switch
        {
            PieceType.Pawn => "",
            PieceType.Knight => "N",
            PieceType.Bishop => "B",
            PieceType.Rook => "R",
            PieceType.Queen => "Q",
            PieceType.King => "K",
            _ => throw new ArgumentOutOfRangeException(nameof(piece), "Invalid piece type")
        };

        private string GetSpecialMoveNotation()
        {
            switch (_move.SpecialMoveType)
            {
                case SpecialMove.Castle:
                    return _move.EndPosition.x == 2 ? "O-O-O" : "O-O";
                case SpecialMove.Promotion:
                    return GetPromotionNotation();
                case SpecialMove.None:
                    return "";
                default:
                    throw new ArgumentOutOfRangeException(nameof(_move.SpecialMoveType), "Invalid special move type");
            }
        }

        private string GetPromotionNotation()
        {
            if (_chessboard.promotedPieces.Count == 0)
            {
                return "";
            }

            var promotedPieceType = _chessboard.promotedPieces.Peek().type;
            return "=" + GetPieceNotation(promotedPieceType);
        }

        public string GeneratePGNString(bool includePGNHeader)
        {
            var moveList = CloneMoveList();
            StringBuilder pgnBuilder = new StringBuilder();

            if (includePGNHeader)
            {
                AppendPGNHeader(pgnBuilder);
            }

            AppendMoves(pgnBuilder, moveList);

            return pgnBuilder.ToString();
        }

        private void AppendPGNHeader(StringBuilder builder)
        {
            builder.AppendLine("[Event \"Chess.NET\"]");
            builder.AppendLine($"[Site \"{SystemInfo.deviceName}\"]");
            builder.AppendLine($"[Date \"{DateTime.Now:yyyy.MM.dd}\"]");
            builder.AppendLine($"[Round \"{Chessboard.NoOfGamesPlayedInSession}\"]");
            builder.AppendLine("[White \"Player1\"]");
            builder.AppendLine("[Black \"Player2\"]");
            builder.AppendLine($"[Result \"{Chessboard.Winner}\"]");
        }

        private void AppendMoves(StringBuilder builder, List<Vector2Int[]> moveList)
        {
            for (int i = 0; i < moveList.Count; i++)
            {
                if (i % 2 == 0)
                {
                    builder.Append($"{i / 2 + 1}. ");
                }

                _move = _chessboard.moveHistory[i];
                foreach (var position in moveList[i])
                {
                    string pgnMove = ConvertToPGN(position, _move.Piece.type);
                    builder.Append($"{pgnMove} ");
                }
            }

            builder.AppendLine();
        }

        public int ExportToPGN()
        {
            try
            {
                var pgn = GeneratePGNString(true);
                Debug.Log(pgn);
                File.WriteAllText("pgn.pgn", pgn);
                return 0; // Success
            }
            catch (Exception ex)
            {
                Debug.LogError($"ExportToPGN failed: {ex.Message}");
                return 1; // Failure
            }
        }

        public string ConvertCurrentMoveToSAN()
        {
            var lastMove = _chessboard?.moveHistory?.LastOrDefault();
            if (lastMove == null)
            {
                throw new InvalidOperationException("No moves available");
            }

            return ConvertToPGN(lastMove.EndPosition, lastMove.Piece.type);
        }
    }
}
