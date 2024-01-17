using System.Collections;
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
        Piece pieceToMove = FindPieceToMove(san, board);
        Debug.Log($"Piece to move: {pieceToMove}");
        Debug.Log($"Start coordinates: ({pieceToMove.currentX}, {pieceToMove.currentY})");


        if (pieceToMove != null)
        {
            (int x, int y) target = SANToBoardCoordinates(san);
            Debug.Log($"Target: {target}");
            Debug.Log($"Target coordinates: ({target.x}, {target.y})");
            Debug.Log($"pieceToMove: {pieceToMove}");
            Debug.Log($"pieceToMoveLocation: ({pieceToMove.currentX}, {pieceToMove.currentY})");
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
        Debug.Log($"Translating SAN to board coordinates: {san}");
        int fileIndex = san.IndexOfAny("abcdefgh".ToCharArray());
        int rankIndex = san.IndexOfAny("12345678".ToCharArray());

        if (fileIndex == -1 || rankIndex == -1)
        {
            Debug.LogError("Invalid SAN format: " + san);
            return (-1, -1);
        }

        // Flipping and rotating the coordinates for file
        int endX = 7 - (san[fileIndex] - 'a');
    
        // Adjusting the rank for the black team (AI)
        int endY = 7 - (san[rankIndex] - '1');

        Debug.Log($"SAN: {san}, endX: {endX}, endY: {endY}");
        return (endX, endY);
    }




    private Piece FindPieceToMove(string san, Chessboard board)
    {
        Debug.Log($"Finding piece to move: {san}");
        PieceType pieceType = IdentifyPieceTypeFromSAN(san);
        Debug.Log($"Piece type: {pieceType}");

        (int targetX, int targetY) = SANToBoardCoordinates(san);

        List<Piece> candidates = new List<Piece>();
        for (int x = 0; x < Chessboard.TILE_COUNT_X; x++)
        {
            for (int y = 0; y < Chessboard.TILE_COUNT_Y; y++)
            {
                Piece piece = board.pieces[x, y];
                if (piece != null && piece.type == pieceType && piece.team == 0) // Assuming 0 is the correct team identifier
                {
                    List<Vector2Int> availableMoves = piece.GetAvailableMoves(ref board.pieces, Chessboard.TILE_COUNT_X, Chessboard.TILE_COUNT_Y);
                    if (availableMoves.Any(move => move.x == targetX && move.y == targetY))
                    {
                        candidates.Add(piece);
                        Debug.Log($"Candidates: {candidates}");
                    }
                }
            }
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
        Debug.Log($"Identifying piece type from SAN: {san}");
        char firstChar = san[0];

        switch (firstChar)
        {
            case 'N':
                return PieceType.Knight;
            case 'B':
                return PieceType.Bishop;
            case 'R':
                return PieceType.Rook;
            case 'Q':
                return PieceType.Queen;
            case 'K':
                return PieceType.King;
            default:
                return PieceType.Pawn;
        }
    }

    //If there are multiple pieces of the same type that can move to the same square,
    //the moving piece is uniquely identified by specifying the piece's letter, followed by (if dsc):
    private Piece DisambiguatePiece(string san, List<Piece> candidates, Chessboard board)
    {
        Debug.Log($"Disambiguating piece: {san}");
        int fileDisambiguation = -1;
        int rankDisambiguation = -1;

        foreach (char c in san)
        {
            if (c >= 'a' && c <= 'h')
            {
                fileDisambiguation = c - 'a';
            }
            else if (c >= '1' && c <= '8')
            {
                rankDisambiguation = c - '1';
            }
        }

        if (fileDisambiguation != -1)
        {
            candidates = candidates.Where(p => p.currentX == fileDisambiguation).ToList();
        }

        if (rankDisambiguation != -1)
        {
            candidates = candidates.Where(p => p.currentY == rankDisambiguation).ToList();
        }

        Debug.Log($"Candidates after disambiguation: {candidates.Count}");
        return candidates.Count == 1 ? candidates[0] : null;
    }
}