using System.Collections.Generic;
using System.Linq;
using ChessNET;
using Pieces;
using UnityEngine;

public class CobraPGNTranslator : MonoBehaviour
{
    public bool TranslateSANAndMove(string san, Chessboard board, bool isWhiteTurn)
    {
        Debug.Log($"Translating SAN and moving: {san}");
        var pieceToMove = FindPieceToMove(san, board);
        Debug.Log($"Piece to move: {pieceToMove}");
        Debug.Log($"Start coordinates: ({pieceToMove.currentX}, {pieceToMove.currentY})");

        if (san is "O-O" or "O-O-O")
        {
            pieceToMove = board.pieces[4, isWhiteTurn ? 7 : 0]; 
            var kingTargetX = san == "O-O" ? 6 : 2; 
            var kingTargetY = isWhiteTurn ? 7 : 0; 

            var rookSourceX = san == "O-O" ? 7 : 0; 
            var rookTargetX = san == "O-O" ? 5 : 3; 

            board.specialMove = SpecialMove.Castle;

            if (board.MoveTo(pieceToMove, kingTargetX, kingTargetY)) 
            {
                var rookToMove = board.pieces[rookSourceX, isWhiteTurn ? 7 : 0]; 
                board.MoveTo(rookToMove, rookTargetX, kingTargetY);
                Debug.Log($"King moved to: ({kingTargetX}, {kingTargetY})");
                Debug.Log($"Rook moved to: ({rookTargetX}, {kingTargetY})");
                return true;
            }
        }


        if (san.Contains('='))
        {
            board.specialMove = SpecialMove.Promotion;
            var promotionIndex = san.IndexOf('=');
            if (promotionIndex != -1 && promotionIndex + 1 < san.Length)
            {
                var promotionChar = san[promotionIndex + 1];
                var promotedPieceType = IdentifyPieceTypeFromSAN(promotionChar.ToString());

                // Determine the position of the pawn being promoted.
                (int x, int y) pawnPosition = SANToBoardCoordinates(san.Substring(0, promotionIndex));
                Debug.Log($"Pawn position for promotion: ({pawnPosition.x}, {pawnPosition.y})");

                // Assuming the pawn to be promoted is at the target position.
                var pawnToPromote = board.pieces[pawnPosition.x, pawnPosition.y];

                // Remove the pawn from the board.
                if (pawnToPromote != null && pawnToPromote.type == PieceType.Pawn)
                    board.pieces[pawnPosition.x, pawnPosition.y].gameObject.SetActive(false);

                // Spawn and position the promoted piece at the pawn's position.
                board.SpawnSinglePiece(promotedPieceType, 0);
                board.PositionSinglePiece(pawnPosition.x, pawnPosition.y);
            }
        }

        if (pieceToMove != null)
        {
            (int x, int y) target = SANToBoardCoordinates(san);
            if (board.MoveTo(pieceToMove, target.x, target.y))
            {
                Debug.Log($"Moved piece to: ({pieceToMove.currentX}, {pieceToMove.currentY})");
                return true;
            }
        }

        return false;
    }

    private (int, int) SANToBoardCoordinates(string san)
    {
        var fileIndex = san.IndexOfAny("abcdefgh".ToCharArray());
        var rankIndex = san.IndexOfAny("12345678".ToCharArray());

        if (fileIndex == -1 || rankIndex == -1)
        {
            Debug.LogError("Invalid SAN format: " + san);
            return (-1, -1);
        }

        // Flipping and rotating the coordinates for file
        var endX = 7 - (san[fileIndex] - 'a');

        // Adjusting the rank for the black team (AI)
        var endY = 7 - (san[rankIndex] - '1');

        Debug.Log($"SAN: {san}, endX: {endX}, endY: {endY}");
        return (endX, endY);
    }

    private Piece FindPieceToMove(string san, Chessboard board)
    {
        Debug.Log($"Finding piece to move: {san}");
        var pieceType = IdentifyPieceTypeFromSAN(san);

        (var targetX, var targetY) = SANToBoardCoordinates(san);

        var candidates = new List<Piece>();
        for (var x = 0; x < Chessboard.TILE_COUNT_X; x++)
        for (var y = 0; y < Chessboard.TILE_COUNT_Y; y++)
        {
            // Find all pieces of the same type that can move to the target position
            var piece = board.pieces[x, y];
            if (piece == null || piece.type != pieceType || piece.team != 0) continue; // only consider white pieces
            var availableMoves = piece.GetAvailableMoves(ref board.pieces, Chessboard.TILE_COUNT_X, Chessboard.TILE_COUNT_Y); // get available moves for the piece
            if (!availableMoves.Any(move =>
                    move.x == targetX &&
                    move.y == targetY)) continue; // if the target position is not in the available moves, skip
            candidates.Add(piece);
            Debug.Log($"Candidates: {candidates}");
        }

        return candidates.Count switch
        {
            //check if disambiguation is needed
            1 => candidates[0],
            > 1 => DisambiguatePiece(san, candidates, board),
            _ => null
        };
    }


    private PieceType IdentifyPieceTypeFromSAN(string san)
    {
        var firstChar = san[0];

        return firstChar switch
        {
            'N' => PieceType.Knight,
            'B' => PieceType.Bishop,
            'R' => PieceType.Rook,
            'Q' => PieceType.Queen,
            'K' => PieceType.King,
            _ => PieceType.Pawn
        };
    }

    //If there are multiple pieces of the same type that can move to the same square,
    //the moving piece is uniquely identified by specifying the piece's letter, followed by (if dsc):
    private Piece DisambiguatePiece(string san, List<Piece> candidates, Chessboard board)
    {
        Debug.Log($"Disambiguating piece: {san}");
        var fileDisambiguation = -1;
        var rankDisambiguation = -1;

        foreach (var c in san)
            switch (c)
            {
                case >= 'a' and <= 'h':
                    fileDisambiguation = c - 'a';
                    break;
                case >= '1' and <= '8':
                    rankDisambiguation = c - '1';
                    break;
            }

        if (fileDisambiguation != -1) candidates = candidates.Where(p => p.currentX == fileDisambiguation).ToList();

        if (rankDisambiguation != -1) candidates = candidates.Where(p => p.currentY == rankDisambiguation).ToList();

        Debug.Log($"Candidates after disambiguation: {candidates.Count}");
        return candidates.Count == 1 ? candidates[0] : null;
    }
}