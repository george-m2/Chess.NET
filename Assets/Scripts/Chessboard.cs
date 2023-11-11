using System.Collections.Generic;
using Pieces;
using UnityEngine;

public enum SpecialMove
{
    None = 0,
    Promotion,
    EnPassant,
    Castle,
}

public class Chessboard : MonoBehaviour
{
    [Header("Graphics")] [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 0.4f;
    [SerializeField] private float yOffset = -0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float takeSize = 0.3f;
    [SerializeField] private float takeSpace = 0.3f;
    [SerializeField] private float dragOffset = 1.5f;
    [SerializeField] private GameObject victoryScreen;

    [Header("Prefabs & Materials")] [SerializeField]
    private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;
    
    private Piece[,] pieces; //x,y array
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8; //creates constant fall back values of grid size
    private Camera currentCamera; //init Unity Camera class which lets the player see the board 
    private GameObject[,] tiles; //Instantiates the base class for all Unity entities
    private Vector2Int currentHover;
    private List<Piece> takenWhitePiece = new();
    private List<Piece> takenBlackPiece = new();
    private Vector3 bounds;
    private Piece currentlyDragging;
    private List<Vector2Int> availableMoves = new();
    private bool isWhiteTurn = true;
    private List<Vector2Int[]>  moveList = new();
    private SpecialMove specialMove;
    

    private void Awake()
    {
        isWhiteTurn = true;
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        //Change when asset imported 
        //Creates a 1 meter grid of 8x8 units on scene start

        SpawnAllPieces();
        PositionAllPieces();
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        var ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            //Get the indexes of tile we hit
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            //If we are hovering any tile after not hovering any tile
            if (currentHover == -Vector2Int.one)
            {currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //if we were already hovering a tile, change previous
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover))
                    ? LayerMask.NameToLayer("Highlight")
                    : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[currentHover.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            if (Input.GetMouseButtonDown(0)) //press down mouse 1
            {
                if (pieces[hitPosition.x, hitPosition.y] != null)
                {
                    if ((pieces[hitPosition.x, hitPosition.y].team == 1 && isWhiteTurn) || (pieces[hitPosition.x, hitPosition.y].team == 0 && !isWhiteTurn))
                    {
                        currentlyDragging = pieces[hitPosition.x, hitPosition.y];
                        //get list of legal moves
                        availableMoves = currentlyDragging.GetAvailableMoves(ref pieces, TILE_COUNT_X, TILE_COUNT_Y);
                        //get list of special moves
                        specialMove = currentlyDragging.GetSpecialMoves(ref pieces, ref moveList, ref availableMoves);
                        HighlightTiles();
                        //highlighted list of legal moves
                    }
                }
            }

            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new(currentlyDragging.currentX, currentlyDragging.currentY);

                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if (!validMove)
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));

                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover))
                    ? LayerMask.NameToLayer("Highlight")
                    : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        if (currentlyDragging)

        {
            Plane horizontalPlane = new(Vector3.up, Vector3.up * yOffset);
            float distance;
            if (horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }
    }

    //board generation
    private void GenerateAllTiles(float tileSize, int tileSizeX, int tileSizeY) //defines the size of the board, and vertical/horizontal length 
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileSizeX / 2) * tileSize, 0, (tileSizeX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileSizeX, tileSizeY];
        for (int x = 0; x < tileSizeX; x++)
        {
            for (int y = 0; y < tileSizeY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y); //nested for loop to generate a chessboard matrix
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new($"X:{x}, Y:{y}"); //generate the tile GameObject
        tileObject.transform.parent = transform; //adds the tile GameObject to the titles GameObject

        Mesh mesh = new();
        tileObject.AddComponent<MeshFilter>().mesh = mesh; //creates a reference to hold a 2D mesh
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial; //render the 2D mesh 

        Vector3[] vertices = new Vector3[4]; //generates 4 vertices for the board
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds; //vertex in each corner of the grid
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        //order for mesh triangle rendering
        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals(); //fixes lighting

        tileObject.layer = LayerMask.NameToLayer("Tile");

        //add raycasting for each tile
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo) //helper function to find the index of x,y
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        for (int y = 0; y < TILE_COUNT_Y; y++)
            if (tiles[x, y] == hitInfo)
                return new Vector2Int(x, y);

        return -Vector2Int.one; //OutOfBounds exception, -1,-1
    }

    //spawn logic 
    private void SpawnAllPieces()
    {
        pieces = new Piece[TILE_COUNT_X, TILE_COUNT_Y];
        int whiteTeam = 0, blackTeam = 1;

        // White
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            pieces[i, 1] = SpawnSinglePiece(PieceType.Pawn, whiteTeam);
        }

        pieces[0, 0] = SpawnSinglePiece(PieceType.Rook, whiteTeam);
        pieces[1, 0] = SpawnSinglePiece(PieceType.Knight, whiteTeam);
        pieces[2, 0] = SpawnSinglePiece(PieceType.Bishop, whiteTeam);
        pieces[3, 0] = SpawnSinglePiece(PieceType.Queen, whiteTeam);  
        pieces[4, 0] = SpawnSinglePiece(PieceType.King, whiteTeam); 
        pieces[5, 0] = SpawnSinglePiece(PieceType.Bishop, whiteTeam);
        pieces[6, 0] = SpawnSinglePiece(PieceType.Knight, whiteTeam);
        pieces[7, 0] = SpawnSinglePiece(PieceType.Rook, whiteTeam);

        //Black
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            pieces[i, 6] = SpawnSinglePiece(PieceType.Pawn, blackTeam);
        }

        pieces[0, 7] = SpawnSinglePiece(PieceType.Rook, blackTeam);
        pieces[1, 7] = SpawnSinglePiece(PieceType.Knight, blackTeam);
        pieces[2, 7] = SpawnSinglePiece(PieceType.Bishop, blackTeam);
        pieces[3, 7] = SpawnSinglePiece(PieceType.Queen, blackTeam);  
        pieces[4, 7] = SpawnSinglePiece(PieceType.King, blackTeam); 
        pieces[5, 7] = SpawnSinglePiece(PieceType.Bishop, blackTeam);
        pieces[6, 7] = SpawnSinglePiece(PieceType.Knight, blackTeam);
        pieces[7, 7] = SpawnSinglePiece(PieceType.Rook, blackTeam);
    }

    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        for (int y = 0; y < TILE_COUNT_Y; y++)
            if (pieces[x, y] != null)
                PositionSinglePiece(x, y, true);
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        pieces[x, y].currentX = x;
        pieces[x, y].currentY = y;
        pieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }

    private Piece SpawnSinglePiece(PieceType type, int team)
    {
        Piece piece = Instantiate(prefabs[(int)type - 1], transform).GetComponent<Piece>();
        piece.type = type;
        piece.team = team;

        if (team == 0) //makes black team face the right way
        {
            piece.transform.Rotate(Vector3.forward, -180);
        }

        piece.GetComponent<MeshRenderer>().material = teamMaterials[team];
        return piece;
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }

    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");

        availableMoves.Clear();
    }

    //endgame logic
    private void CheckMate(int team)
    {
        DisplayWin(team);
    }

    private void DisplayWin(int winningTeam)
    {
        victoryScreen.SetActive(true);

        for (int i = 0; i < victoryScreen.transform.childCount; i++)
        {
            GameObject childObject = victoryScreen.transform.GetChild(i).gameObject;

            // Exclude buttons from deactivation
            if (i == winningTeam || i == 2 || i == 3)
            {
                childObject.SetActive(true);
            }
            else
            {
                childObject.SetActive(false);
            }
        }
    }

    public void Restart()
    {
        //Field reset
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

        //UI Reset
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        //Scene clean up
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (pieces[x, y] != null)
                {
                    Destroy(pieces[x, y].gameObject);
                    pieces[x, y] = null;
                }
            }
        }

        for (int i = 0; i < takenWhitePiece.Count; i++)
            Destroy(takenWhitePiece[i].gameObject);
        for (int i = 0; i < takenBlackPiece.Count; i++)
            Destroy(takenBlackPiece[i].gameObject);

        takenWhitePiece.Clear();
        takenBlackPiece.Clear();

        //reinitialise board
        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }

    public void Quit()
    {
        Application.Quit();
    }
    
    //special move logic
    private void ProcessSpecialMoves()
    {
        if (specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[^1];
            Piece playerPawn = pieces[newMove[1].x, newMove[1].y];
            var targetPawnPos = moveList[^2];
            var enemyPawn = pieces[targetPawnPos[1].x, targetPawnPos[1].y];

            if (playerPawn.currentX == enemyPawn.currentX)
            {
                if (playerPawn.currentY == enemyPawn.currentY - 1 || playerPawn.currentY == enemyPawn.currentY + 1)
                {
                    if (enemyPawn.team == 0)
                        takenWhitePiece.Add(enemyPawn);
                    enemyPawn.SetScale(Vector3.one * takeSize);
                    enemyPawn.SetPosition(
                        new Vector3(9 * tileSize, yOffset, -1 * tileSize)
                        - bounds
                        + new Vector3(tileSize / 2, 0, tileSize / 2)
                        + (Vector3.forward * takeSpace) * takenWhitePiece.Count);
                }
                else
                {
                    takenBlackPiece.Add(enemyPawn);
                    enemyPawn.SetScale(Vector3.one * takeSize);
                    enemyPawn.SetPosition(
                        new Vector3(-2 * tileSize, yOffset, 9 * tileSize)
                        - bounds
                        + new Vector3(tileSize / 2, 0, tileSize / 2)
                        + (Vector3.back * takeSpace) * takenBlackPiece.Count);
                }
                pieces[enemyPawn.currentX, enemyPawn.currentY] = null;
            }
            
        }
        
        if (specialMove == SpecialMove.Castle)
        {
            Vector2Int[] lastMove = moveList[^1];
            
            //left rook
            if (lastMove[1].x == 2)
            {
                //white side
                if (lastMove[1].y == 0)
                {
                    Piece rook = pieces[0, 0];
                    pieces[3, 0] = rook;
                    PositionSinglePiece(3, 0); //position new rook
                    pieces[0, 0] = null; //delete old rook
                }

                //black side
                else if (lastMove[1].y == 7)
                {
                    Piece rook = pieces[0, 7];
                    pieces[3, 7] = rook;
                    PositionSinglePiece(3, 7);
                    pieces[0, 7] = null;
                }
            }

            //right rook
            else if (lastMove[1].x == 6)
            {
                //white side
                if (lastMove[1].y == 0) //right, white
                {
                    Piece rook = pieces[7, 0];
                    pieces[5, 0] = rook;
                    PositionSinglePiece(5, 0); 
                    pieces[7, 0] = null; 
                }

                //black side
                else if (lastMove[1].y == 7)
                {
                    Piece rook = pieces[7, 7];
                    pieces[5, 7] = rook;
                    PositionSinglePiece(5, 7);
                    pieces[7, 7] = null;
                }
            }
                
        }

        if (specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[^1];
            Piece pawn = pieces[lastMove[1].x, lastMove[1].y];

            if (pawn.type == PieceType.Pawn)
            {
                if (pawn.team == 0 && lastMove[1].y == 7) //white promote
                {
                    Piece promotedQueen = SpawnSinglePiece(PieceType.Queen, 0);
                    promotedQueen.transform.position = pieces[lastMove[1].x, lastMove[1].y].transform.position; //smoother pawn to queen transition
                    //destroy pawn, spawn piece
                    Destroy(pieces[lastMove[1].x, lastMove[1].y].gameObject);
                    pieces[lastMove[1].x, lastMove[1].y] = promotedQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                    
                }
                if (pawn.team == 1 && lastMove[1].y == 0) //black promote
                {
                    Piece promotedQueen = SpawnSinglePiece(PieceType.Queen, 0);
                    Destroy(pieces[lastMove[1].x, lastMove[1].y].gameObject);
                    pieces[lastMove[1].x, lastMove[1].y] = promotedQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                    
                }
            }
        }

    }

    //movement logic
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;
        return false;
    }

    private bool MoveTo(Piece cp, int x, int y)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2Int(x, y)))
            return false;

        Vector2Int previousPosition = new(cp.currentX, cp.currentY);

        if (pieces[x, y] != null)
        {
            Piece ocp = pieces[x, y];

            if (cp.team == ocp.team)
            {
                return false;
            }

            if (ocp.team == 0)
            {
                if (ocp.type == PieceType.King)
                {
                    CheckMate(1); //white wins
                }

                takenWhitePiece.Add(ocp);
                ocp.SetScale(Vector3.one * takeSize);
                ocp.SetPosition(
                    new Vector3(9 * tileSize, yOffset, -1 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * takeSpace) * takenWhitePiece.Count);
            }

            else
            {
                if (ocp.type == PieceType.King)
                {
                    CheckMate(0); //black wins
                }

                takenBlackPiece.Add(ocp);
                ocp.SetScale(Vector3.one * takeSize);
                ocp.SetPosition(
                    new Vector3(-2 * tileSize, yOffset, 9 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * takeSpace) * takenBlackPiece.Count);
            }
        }

        pieces[x, y] = cp;
        pieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        isWhiteTurn = !isWhiteTurn; //switches turn
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y)});

        ProcessSpecialMoves();
        return true;
}
}