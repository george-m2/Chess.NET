using System;
using System.Collections.Generic;
using System.Linq;
using Communication;
using GameUIManager;
using Pieces;
using UnityEngine;
using UnityEngine.UI;

namespace ChessNET
{
    public enum SpecialMove 
    {
        None = 0,
        Promotion,
        EnPassant,
        Castle
    }

    public class Chessboard : MonoBehaviour
    {
        public const int TILE_COUNT_X = 8;
        public const int TILE_COUNT_Y = 8; //creates constant fall back values of grid size
        public static int NoOfGamesPlayedInSession = 1; 
        public static string Winner = "*"; 
        [Header("Graphics")] [SerializeField] private Material tileMaterial;
        [SerializeField] private float tileSize = 0.4f;
        [SerializeField] private float yOffset = -0.2f;
        [SerializeField] private Vector3 boardCenter = Vector3.zero;
        [SerializeField] private float takeSize = 0.3f;
        [SerializeField] private float takeSpace = 0.3f;
        [SerializeField] private float dragOffset = 1.5f;
        [SerializeField] private GameObject victoryScreen;
        [SerializeField] private GameObject whitePromotion;
        [SerializeField] private GameObject blackPromotion;

        [Header("Prefabs & Materials")] [SerializeField]
        private GameObject[] prefabs; //array of prefabs for each piece

        [SerializeField] private Material[] teamMaterials; //array of materials for each team

        [Header("UI Elements")] [SerializeField]
        public Button buttonWhiteQueen;

        [SerializeField] public Button buttonWhiteRook;
        [SerializeField] public Button buttonWhiteBishop;
        [SerializeField] public Button buttonWhiteKnight;
        [SerializeField] public Button buttonBlackQueen;
        [SerializeField] public Button buttonBlackRook;
        [SerializeField] public Button buttonBlackBishop;
        [SerializeField] public Button buttonBlackKnight;
        public new AudioSource audio; //init Unity AudioSource class which lets the player hear SFX/music
        public AudioClip checkSfx;
        public AudioClip checkMateSfx;
        public AudioClip staleMateSfx;
        public UIManager UIManager;
        public Client client;
        public bool isWhiteTurn = true;
        public SpecialMove specialMove;
        private readonly List<Piece> takenBlackPiece = new();
        private readonly List<Piece> takenWhitePiece = new();
        private List<Vector2Int> availableMoves = new();
        private Vector3 bounds;
        private Camera currentCamera; //init Unity Camera class which lets the player see the board 
        private Vector2Int currentHover;
        private Piece currentlyDragging;
        internal List<Move> moveHistory = new();
        private int moveIndex = -1;
        private List<Vector2Int[]> moveList = new();
        private readonly Stack<Piece> originalPieces = new();

        internal Piece[,] pieces; //x,y array. Automatic properties needed for PGNExporter
        public Stack<Piece> promotedPieces = new();
        private GameObject[,] tiles; //Instantiates the base class for all Unity entities


        private void Awake()
        {
            audio = GetComponent<AudioSource>(); 
            client = FindObjectOfType<Client>(); 
            UIManager = FindObjectOfType<UIManager>();
            isWhiteTurn = true;
            GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
            //Change when asset imported 
            //Creates a 1 meter grid of 8x8 units on scene start
            SpawnAllPieces();
            PositionAllPieces();
        }

        private void Update()
        {
            if (!currentCamera) //if no camera is found, use the main camera
            {
                currentCamera = Camera.main;
                return;
            }

            if (UIManager.ResignPanel.gameObject.activeSelf) return; //if the resign panel is active, return

            var ray = currentCamera.ScreenPointToRay(Input.mousePosition); //creates a ray from the camera to the mouse position

            if (Physics.Raycast(ray, out var info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))  //if the ray hits a tile, hover, or highlight layer
            {
                if (moveIndex != moveHistory.Count - 1) //if the user is attempting to move back when they are not at the current move list
                {
                    if (Input.GetMouseButtonDown(0)) UIManager.StartCoroutine(UIManager.ShowAndHideAlertPanel(UIManager.alertPanel, 1.0f)); //if the mouse is clicked, show the alert panel
                    return;
                }

                //Get the indexes of tile we hit
                var hitPosition = LookupTileIndex(info.transform.gameObject);

                //If we are hovering any tile after not hovering any tile
                if (currentHover == -Vector2Int.one)
                {
                    currentHover = hitPosition;
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                }

                //if we were already hovering a tile, change previous
                if (currentHover != hitPosition)
                {
                    tiles[currentHover.x, currentHover.y].layer = ContainsValidMove(ref availableMoves, currentHover) //is the move valid?
                        ? LayerMask.NameToLayer("Highlight") //if yes, highlight the tile
                        : LayerMask.NameToLayer("Tile"); //if no, return the tile to normal
                    currentHover = hitPosition; //set the current hover to the new tile
                    tiles[currentHover.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); //highlight the new tile
                }

                if (Input.GetMouseButtonDown(0)) //press down mouse 1
                    if (pieces[hitPosition.x, hitPosition.y] != null) //if the tile is not empty
                    //if the piece is the same team as the player
                        if ((pieces[hitPosition.x, hitPosition.y].team == 1 && isWhiteTurn) ||
                            (pieces[hitPosition.x, hitPosition.y].team == 0 && !isWhiteTurn)) 
                        {
                            currentlyDragging = pieces[hitPosition.x, hitPosition.y]; //set the piece to be dragged
                            //get list of legal moves
                            availableMoves = currentlyDragging.GetAvailableMoves(ref pieces, TILE_COUNT_X, TILE_COUNT_Y);
                            //get list of special moves
                            specialMove = currentlyDragging.GetSpecialMoves(ref pieces, ref moveList, ref availableMoves);
                            PreventCheck();
                            HighlightTiles();
                            //highlighted list of legal moves
                        }

                if (currentlyDragging != null && Input.GetMouseButtonUp(0)) //if the piece is being dragged and the mouse is released
                {
                    //get the previous position of the piece
                    Vector2Int previousPosition = new(currentlyDragging.currentX, currentlyDragging.currentY);

                    //get the position of the tile we are hovering
                    var validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                    if (!validMove)
                        //if the move is not valid, return the piece to its original position
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y)); 
                    currentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }

            else
            {
                if (currentHover != -Vector2Int.one) //if we are hovering a tile
                {
                    tiles[currentHover.x, currentHover.y].layer = ContainsValidMove(ref availableMoves, currentHover) //is the move valid?
                        ? LayerMask.NameToLayer("Highlight")
                        : LayerMask.NameToLayer("Tile");
                    currentHover = -Vector2Int.one;
                }

                if (currentlyDragging && Input.GetMouseButtonUp(0))
                {
                    currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX,
                        currentlyDragging.currentY));
                    currentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }

            if (!currentlyDragging) return;
            Plane horizontalPlane = new(Vector3.up, Vector3.up * yOffset); //creates a plane to drag the piece on
            if (horizontalPlane.Raycast(ray, out var distance)) 
                //if the ray hits the plane, move the piece to the ray hit point
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset); 
        }

        //board generation
        private void GenerateAllTiles(float tileSize, int tileSizeX, int tileSizeY) //defines the size of the board, and vertical/horizontal length 
        {
            yOffset += transform.position.y; //offsets the board to the correct height
            bounds = new Vector3(tileSizeX / 2 * tileSize, 0, tileSizeX / 2 * tileSize) + boardCenter; //sets the bounds of the board

            //nested for loop to generate a chessboard matrix
            tiles = new GameObject[tileSizeX, tileSizeY]; 
            for (var x = 0; x < tileSizeX; x++)
            for (var y = 0; y < tileSizeY; y++) 
                tiles[x, y] = GenerateSingleTile(tileSize, x, y); 
        }

        private GameObject GenerateSingleTile(float tileSize, int x, int y)
        {
            GameObject tileObject = new($"X:{x}, Y:{y}"); //generate the tile GameObject
            tileObject.transform.parent = transform; //adds the tile GameObject to the titles GameObject

            Mesh mesh = new();
            tileObject.AddComponent<MeshFilter>().mesh = mesh; //creates a reference to hold a 2D mesh
            tileObject.AddComponent<MeshRenderer>().material = tileMaterial; //render the 2D mesh 

            var vertices = new Vector3[4]; //generates 4 vertices for the board
            vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
            vertices[1] =
                new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds; //vertex in each corner of the grid
            vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
            vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

            //order for mesh triangle rendering
            int[] tris = { 0, 1, 2, 1, 3, 2 };
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
            for (var x = 0; x < TILE_COUNT_X; x++) 
            for (var y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

            return -Vector2Int.one; //OutOfBounds exception, -1,-1
        }

        //spawn logic 
        private void SpawnAllPieces()
        {
            pieces = new Piece[TILE_COUNT_X, TILE_COUNT_Y]; //2D array of pieces
            const int whiteTeam = 0;
            const int blackTeam = 1;

            // White
            for (var i = 0; i < TILE_COUNT_X; i++) pieces[i, 1] = SpawnSinglePiece(PieceType.Pawn, whiteTeam); //spawn white pawns

            pieces[0, 0] = SpawnSinglePiece(PieceType.Rook, whiteTeam);
            pieces[1, 0] = SpawnSinglePiece(PieceType.Knight, whiteTeam);
            pieces[2, 0] = SpawnSinglePiece(PieceType.Bishop, whiteTeam);
            pieces[3, 0] = SpawnSinglePiece(PieceType.King, whiteTeam);
            pieces[4, 0] = SpawnSinglePiece(PieceType.Queen, whiteTeam);
            pieces[5, 0] = SpawnSinglePiece(PieceType.Bishop, whiteTeam);
            pieces[6, 0] = SpawnSinglePiece(PieceType.Knight, whiteTeam);
            pieces[7, 0] = SpawnSinglePiece(PieceType.Rook, whiteTeam);

            //Black
            for (var i = 0; i < TILE_COUNT_X; i++) pieces[i, 6] = SpawnSinglePiece(PieceType.Pawn, blackTeam); //spawn black pawns

            pieces[0, 7] = SpawnSinglePiece(PieceType.Rook, blackTeam);
            pieces[1, 7] = SpawnSinglePiece(PieceType.Knight, blackTeam);
            pieces[2, 7] = SpawnSinglePiece(PieceType.Bishop, blackTeam);
            pieces[3, 7] = SpawnSinglePiece(PieceType.King, blackTeam);
            pieces[4, 7] = SpawnSinglePiece(PieceType.Queen, blackTeam);
            pieces[5, 7] = SpawnSinglePiece(PieceType.Bishop, blackTeam);
            pieces[6, 7] = SpawnSinglePiece(PieceType.Knight, blackTeam);
            pieces[7, 7] = SpawnSinglePiece(PieceType.Rook, blackTeam);
        }

        private void PositionAllPieces()
        {
            for (var x = 0; x < TILE_COUNT_X; x++)
            for (var y = 0; y < TILE_COUNT_Y; y++)
                if (pieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
        }

        internal void PositionSinglePiece(int x, int y, bool force = false)
        {
            //set the current x and y of the piece
            pieces[x, y].currentX = x; 
            pieces[x, y].currentY = y;
            pieces[x, y].SetPosition(GetTileCenter(x, y), force); //force the piece to the center of the tile
        }

        public Piece SpawnSinglePiece(PieceType type, int team)
        {
            var piece = Instantiate(prefabs[(int)type - 1], transform).GetComponent<Piece>(); //instantiate the piece
            piece.type = type;
            piece.team = team;

            if (team == 0) //makes black team face the right way
                piece.transform.Rotate(Vector3.forward, -180);

            piece.GetComponent<MeshRenderer>().material = teamMaterials[team]; //set the material of the piece based on team
            return piece;
        }

        private Vector3 GetTileCenter(int x, int y)
        {
            return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + //get the center of the tile
                   new Vector3(tileSize / 2, 0, tileSize / 2);  //offset the piece to the center of the tile
        }

        private void HighlightTiles()
        {
            foreach (var t in availableMoves) //highlight all available moves
                tiles[t.x, t.y].layer = LayerMask.NameToLayer("Highlight");
        }

        private void RemoveHighlightTiles()
        {
            foreach (var t in availableMoves) //remove all highlights
                tiles[t.x, t.y].layer = LayerMask.NameToLayer("Tile");

            availableMoves.Clear();
        }

        //endgame logic
        private void CheckMate(int team)
        {
            DisplayWin(team);
        }

        private void DisplayWin(int winningTeam)
        {
            switch (winningTeam)
            {
                case 0:
                    audio.PlayOneShot(checkMateSfx, 1F); //play checkmate sound effect
                    Winner = "Black";
                    break;
                case 1:
                    audio.PlayOneShot(checkMateSfx, 1F);
                    Winner = "White";
                    break;
                case 2:
                    audio.PlayOneShot(staleMateSfx, 1F); //play stalemate sound effect
                    Winner = "Stalemate";
                    break;
            }

            victoryScreen.SetActive(true);
            victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true); //display the victory screen
        }

        public void Restart()
        {
            //Field reset
            currentlyDragging = null;
            availableMoves.Clear();
            moveList.Clear();
            moveIndex = -1;
            moveHistory.Clear();
            promotedPieces.Clear();
            originalPieces.Clear();
            NoOfGamesPlayedInSession++;
            client.KillCobraProcess();

            //UI Reset
            victoryScreen.transform.GetChild(2).gameObject.SetActive(false);
            victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
            victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
            victoryScreen.SetActive(false);

            //Scene clean up
            for (var x = 0; x < TILE_COUNT_X; x++)
            for (var y = 0; y < TILE_COUNT_Y; y++)
                if (pieces[x, y] != null)
                {
                    Destroy(pieces[x, y].gameObject);
                    pieces[x, y] = null;
                }

            foreach (var t in takenWhitePiece)
                Destroy(t.gameObject);

            foreach (var t in takenBlackPiece)
                Destroy(t.gameObject);

            takenWhitePiece.Clear();
            takenBlackPiece.Clear();

            //reinitialise board
            SpawnAllPieces();
            PositionAllPieces();
            isWhiteTurn = true;
            client.CreateEngineProcess();
        }

        public void Quit()
        {
            Application.Quit();
            client.KillCobraProcess();
        }

        //special move logic
        private void ProcessSpecialMoves()
        {
            if (specialMove == SpecialMove.EnPassant) HandleEnPassantMove();

            if (specialMove == SpecialMove.Castle) HandleCastling();

            if (specialMove != SpecialMove.Promotion) return;
            {
                var lastMove = moveList[^1];
                var pawn = pieces[lastMove[1].x, lastMove[1].y]; //get the pawn
                originalPieces.Push(pawn);

                if (pawn.type != PieceType.Pawn) return;
                if (pawn.team == 0 && lastMove[1].y == 7) //black promote
                {
                    blackPromotion.SetActive(true);
                    buttonBlackQueen.onClick.AddListener(() =>
                    {
                        PromotePawn(0, lastMove[1], PieceType.Queen);
                        blackPromotion.SetActive(false); //hide promotion panel
                    });
                    buttonBlackRook.onClick.AddListener(() =>
                    {
                        PromotePawn(0, lastMove[1], PieceType.Rook);
                        blackPromotion.SetActive(false);
                    });
                    buttonBlackBishop.onClick.AddListener(() =>
                    {
                        PromotePawn(0, lastMove[1], PieceType.Bishop);
                        blackPromotion.SetActive(false);
                    });
                    buttonBlackKnight.onClick.AddListener(() =>
                    {
                        PromotePawn(0, lastMove[1], PieceType.Knight);
                        blackPromotion.SetActive(false);
                    });
                }

                if (pawn.team != 1 || lastMove[1].y != 0) return; //white promote
                whitePromotion.SetActive(true);
                buttonWhiteQueen.onClick.AddListener(() =>
                {
                    PromotePawn(1, lastMove[1], PieceType.Queen);
                    whitePromotion.SetActive(false);
                });
                buttonWhiteRook.onClick.AddListener(() =>
                {
                    PromotePawn(1, lastMove[1], PieceType.Rook);
                    whitePromotion.SetActive(false);
                });
                buttonWhiteBishop.onClick.AddListener(() =>
                {
                    PromotePawn(1, lastMove[1], PieceType.Bishop);
                    whitePromotion.SetActive(false);
                });
                buttonWhiteKnight.onClick.AddListener(() =>
                {
                    PromotePawn(1, lastMove[1], PieceType.Knight);
                    whitePromotion.SetActive(false);
                });
            }
        }

        private void HandleCastling()
        {
            var lastMove = moveList[^1]; //get the last move

            switch (lastMove[1].x) //king move x value
            {
                case 5: // queen-side castling
                    if (lastMove[1].y == 0) // black
                    {
                        var rook = pieces[7, 0];
                        pieces[4, 0] = rook; 
                        PositionSinglePiece(4, 0);
                        pieces[7, 0] = null; 
                    }
                    else if (lastMove[1].y == 7) // white
                    {
                        var rook = pieces[7, 7];
                        pieces[4, 7] = rook; 
                        PositionSinglePiece(4, 7);
                        pieces[7, 7] = null; 
                    }
                    break;
                case 1:  //king-side castling
                    if (lastMove[1].y == 0) // black
                    {
                        var rook = pieces[0, 0];
                        pieces[2, 0] = rook; 
                        PositionSinglePiece(2, 0);
                        pieces[0, 0] = null; 
                    }
                    else if (lastMove[1].y == 7) // white
                    {
                        var rook = pieces[0, 7];
                        pieces[2, 7] = rook; 
                        PositionSinglePiece(2, 7);
                        pieces[0, 7] = null; 
                    }
                    break;
            }
        }


        private void HandleEnPassantMove()
        {
            var newMove = moveList[^1];
            var playerPawn = pieces[newMove[1].x, newMove[1].y]; //get the pawn
            var targetPawnPos = moveList[^2]; //get the target pawn position
            var enemyPawn = pieces[targetPawnPos[1].x, targetPawnPos[1].y]; //get the target pawn

            if (playerPawn.currentX != enemyPawn.currentX) return;
            if (playerPawn.currentY != enemyPawn.currentY - 1 && playerPawn.currentY != enemyPawn.currentY + 1) return; //if the pawn is not in the correct position
            if (enemyPawn.team == 0)
            {
                takenWhitePiece.Add(enemyPawn);
                enemyPawn.SetScale(Vector3.one * takeSize); 
                enemyPawn.SetPosition(
                    new Vector3(9 * tileSize, yOffset, -2 * tileSize) - bounds + //set the position of the taken piece
                    new Vector3(tileSize / 2, 0, tileSize / 2) + //offset the piece to the center of the tile
                    Vector3.forward * (takeSpace * takenWhitePiece.Count)); 
            }
            else
            {
                takenBlackPiece.Add(enemyPawn);
                enemyPawn.SetScale(Vector3.one * takeSize);
                enemyPawn.SetPosition(
                    new Vector3(-2 * tileSize, yOffset, 9 * tileSize) - bounds +
                    new Vector3(tileSize / 2, 0, tileSize / 2) +
                    Vector3.back * (takeSpace * takenBlackPiece.Count));
            }

            pieces[enemyPawn.currentX, enemyPawn.currentY] = null; //remove the piece from the board
        }

        private void PromotePawn(int team, Vector2Int lastMovePosition, PieceType promotionType)
        {
            var currentPromote = SpawnSinglePiece(promotionType, team); //spawn the promoted piece
            currentPromote.transform.position = pieces[lastMovePosition.x, lastMovePosition.y].transform.position; //set the position of the promoted piece
            pieces[lastMovePosition.x, lastMovePosition.y].gameObject.SetActive(false);
            pieces[lastMovePosition.x, lastMovePosition.y] = currentPromote;
            PositionSinglePiece(lastMovePosition.x, lastMovePosition.y);
            promotedPieces.Push(currentPromote);
        }

        private void PreventCheck()
        {
            Piece targetKing = null;
            for (var x = 0; x < TILE_COUNT_X; x++)
            for (var y = 0; y < TILE_COUNT_Y; y++)
                if (pieces[x, y] != null)
                    if (pieces[x, y].type == PieceType.King)
                        if (pieces[x, y].team == currentlyDragging.team) //get the target king
                            targetKing = pieces[x, y];
            //ref availableMoves, moves putting us in check are deleted from the list
            SimMoveForPiece(currentlyDragging, ref availableMoves, targetKing);
        }

        private void SimMoveForPiece(Piece cp, ref List<Vector2Int> moves, Piece targetKing)
        {
            //saves values to reset after call
            var actualX = cp.currentX;
            var actualY = cp.currentY;
            var movesToRemove = new List<Vector2Int>();

            //go through all moves, sim moves and check if king is in check
            foreach (var t in moves)
            {
                var simX = t.x;
                var simY = t.y;

                var kingSimPos = new Vector2Int(targetKing.currentX, targetKing.currentY);
                //has king move been simulated?
                if (cp.type == PieceType.King)
                    kingSimPos = new Vector2Int(simX, simY);

                //copy piece array 
                var simulation = new Piece[TILE_COUNT_X, TILE_COUNT_Y];
                var simAttackPiece = new List<Piece>();
                for (var x = 0; x < TILE_COUNT_X; x++)
                for (var y = 0; y < TILE_COUNT_Y; y++)
                    if (pieces[x, y] != null) 
                    {
                        simulation[x, y] = pieces[x, y];
                        if (simulation[x, y].team != cp.team)
                            simAttackPiece.Add(simulation[x, y]); //add attacking pieces to list
                    }

                //simulate move
                simulation[actualX, actualY] = null;
                cp.currentX = simX;
                cp.currentY = simY;
                simulation[simX, simY] = cp; 

                //did a piece get taken in sim?
                var deadPiece = simAttackPiece.Find(p => p.currentX == simX && p.currentY == simY); //find the taken piece
                if (deadPiece != null)
                    simAttackPiece.Remove(deadPiece);

                //get all the moves for the simulated attacking pieces 
                var simMoves = new List<Vector2Int>();
                foreach (var pieceMoves in simAttackPiece.Select(t1 => t1.GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y))) //get all moves for attacking pieces
                {
                    simMoves.AddRange(pieceMoves);
                }

                //Is in the king in check?
                if (ContainsValidMove(ref simMoves, kingSimPos)) movesToRemove.Add(t);

                //restore values
                cp.currentX = actualX;
                cp.currentY = actualY;
            }

            //remove from move list
            foreach (var t in movesToRemove)
                moves.Remove(t);
        } 
        
        private bool AreOnlyKingsLeft()
        {
            int pieceCount = 0;
            //check if only two pieces are left on the board
            for (var x = 0; x < TILE_COUNT_X; x++)
            {
                for (var y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (pieces[x, y] == null) continue; // if no piece is found, continue.
                    pieceCount++;
                    // if any piece besides a king is found, return false.
                    if (pieces[x, y].type != PieceType.King)
                    {
                        return false;
                    }
                }
            }
            // true if only two pieces (kings) are on the board, false otherwise.
            return pieceCount == 2;
        }

        private int CheckForCheckmate()
        {
            var lastMove = moveList[^1];
            var targetTeam = pieces[lastMove[1].x, lastMove[1].y].team == 0 ? 1 : 0; //get the team

            var attackingPieces = new List<Piece>();
            var defendingPieces = new List<Piece>();
            Piece targetKing = null;
            //get all attacking and defending pieces
            for (var x = 0; x < TILE_COUNT_X; x++)
            for (var y = 0; y < TILE_COUNT_Y; y++)
                if (pieces[x, y] != null)
                {
                    if (pieces[x, y].team == targetTeam)
                    {
                        defendingPieces.Add(pieces[x, y]);
                        if (pieces[x, y].type == PieceType.King)
                            targetKing = pieces[x, y];
                    }

                    else
                    {
                        attackingPieces.Add(pieces[x, y]);
                    }
                }
            
            if (AreOnlyKingsLeft()) // If only kings are left on the board, it's a stalemate.
            {
                return 2; // stalemate
            }

            //Is the king being attacked?
            var currentAvailableMoves = new List<Vector2Int>();
            foreach (var pieceMoves in attackingPieces.Select(t => t.GetAvailableMoves(ref pieces, TILE_COUNT_X, TILE_COUNT_Y)))
            {
                currentAvailableMoves.AddRange(pieceMoves);
            }

            // Are we in check?
            if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
            {
                //Can we block?
                foreach (var t in defendingPieces)
                {
                    var defendingMoves =
                        t.GetAvailableMoves(ref pieces, TILE_COUNT_X, TILE_COUNT_Y); //get all moves for defending pieces 
                    SimMoveForPiece(t, ref defendingMoves, targetKing); 

                    //check, not checkmate
                    if (defendingMoves.Count == 0) continue;
                    audio.PlayOneShot(checkSfx, 1F); //sfx for check
                    return 0; //not checkmate
                }

                return 1; //checkmate condition
            }

            foreach (var t in defendingPieces)
            {
                var defendingMoves =
                    t.GetAvailableMoves(ref pieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimMoveForPiece(t, ref defendingMoves, targetKing);
                if (defendingMoves.Count != 0)
                    return 0; //can be defended
            }

            return 2; //stalemate condition
        }

        //movement logic
        private static bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
        {
            return moves.Any(t => t.x == pos.x && t.y == pos.y); //checks if a move is valid
        }

        public bool MoveTo(Piece cp, int x, int y)
        {
            if (isWhiteTurn)
                if (!ContainsValidMove(ref availableMoves, new Vector2Int(x, y))) //if the move is not valid
                {
                    Debug.LogError($"cannot move {cp} to {x},{y}");
                    return false;
                }

            var move = new Move
            {
                StartPosition = new Vector2Int(cp.currentX, cp.currentY),
                EndPosition = new Vector2Int(x, y),
                Piece = cp,
                TakenPiece = pieces[x, y],
                SpecialMoveType = specialMove
            };

            if (move.SpecialMoveType == SpecialMove.EnPassant)
                move.TakenPiece = pieces[x, y - 1];
            
            Vector2Int previousPosition = new(cp.currentX, cp.currentY);

            if (pieces[x, y] != null)
            {
                var ocp = pieces[x, y]; //get the piece at the target position

                if (cp.team == ocp.team) return false;

                move.isCapture = true;
                if (ocp.team == 0)
                {
                    if (ocp.type == PieceType.King) CheckMate(1); //white wins

                    pieces[ocp.currentX, ocp.currentY] = null; //ensure taken piece is removed
                    var offBoardPosition = new Vector3(9 * tileSize, yOffset, -1 * tileSize)
                                           - bounds
                                           + new Vector3(tileSize / 2, 0, tileSize / 2)
                                           + Vector3.forward * (takeSpace * takenWhitePiece.Count);
                    ocp.SetPosition(offBoardPosition);
                    ocp.SetScale(Vector3.one * takeSize);
                    move.OffBoardPosition =
                        offBoardPosition; //allows pieces to return off the board when we cycle through the move list 
                    takenWhitePiece.Add(ocp);
                }

                else
                {
                    if (ocp.type == PieceType.King) CheckMate(0); //black wins

                    pieces[ocp.currentX, ocp.currentY] = null;
                    //set the position of the taken piece
                    var offBoardPosition = new Vector3(-2 * tileSize, yOffset, 8 * tileSize)
                                           - bounds
                                           + new Vector3(tileSize / 2, 0, tileSize / 2)
                                           + Vector3.back * (takeSpace * takenBlackPiece.Count); 
                    ocp.SetPosition(offBoardPosition);
                    ocp.SetScale(Vector3.one * takeSize);
                    move.OffBoardPosition = offBoardPosition;
                    takenBlackPiece.Add(ocp);
                }
            }

            pieces[x, y] = cp;
            pieces[previousPosition.x, previousPosition.y] = null;

            PositionSinglePiece(x, y);

            isWhiteTurn = !isWhiteTurn; //switches turn
            moveList.Add(new[] { previousPosition, new Vector2Int(x, y) });
            moveHistory.Add(move);
            moveIndex = moveHistory.Count - 1;

            switch (CheckForCheckmate())
            {
                case 1:
                    CheckMate(cp.team);
                    client.SendGameOver(UIManager.HandleBestMoveNumber, UIManager.HandleBlunderNumber); //get game over data
                    break;
                case 2:
                    CheckMate(2); //stalemate
                    client.SendGameOver(UIManager.HandleBestMoveNumber, UIManager.HandleBlunderNumber); 
                    break;
            }

            ProcessSpecialMoves();
            UIManager.UpdatePGNText();
            OnMoveMade();
            cp.hasMoved = true; 
            return true;
        }

        private void OnMoveMade()
        {
            // Currently, AI is always black
            if (!isWhiteTurn) Client.Instance.ReceiveMoveData(HandlePGN, HandleACPL);
        }

        private void HandlePGN(string pgn) //callback for PGN
        {
            Debug.Log(pgn);
        }

        private void HandleACPL(int acpl) //callback for ACPL
        {
            Debug.Log(acpl);
        }

        public void ProcessReceivedMove(string sanMove)
        {
            var translator = FindObjectOfType<CobraPGNTranslator>(); 
            if (translator != null)
            {
                var moveSuccess = translator.TranslateSANAndMove(sanMove, this, isWhiteTurn); //translate and execute move
                if (!moveSuccess) Debug.LogError("Failed to translate or execute move: " + sanMove);
            }
            else
            {
                Debug.LogError("CobraPGNTranslator component not found.");
            }
        }

        //*************************************************************************//
        //Move History logic

        private void UndoMove(Move move)
        {
            // helper method for undoing a move
            pieces[move.StartPosition.x, move.StartPosition.y] = move.Piece;
            pieces[move.EndPosition.x, move.EndPosition.y] = move.TakenPiece;
            PositionSinglePiece(move.StartPosition.x, move.StartPosition.y);

            HandleTakenPiece(move);
        }

        private void HandleTakenPiece(Move move)
        {
            // helper method for undoing a capture 
            if (move.TakenPiece != null)
            {
                PositionSinglePiece(move.EndPosition.x, move.EndPosition.y); // position the taken piece back
                if (move.TakenPiece.team == 0)
                    takenWhitePiece.Remove(move.TakenPiece);
                else
                    takenBlackPiece.Remove(move.TakenPiece);
            }
        }

        public void MoveBack()
        {
            if (moveIndex < 0) return; // No move to go back to

            // Get the move to undo
            var move = moveHistory[moveIndex];

            // Check the type of special move
            switch (move.SpecialMoveType)
            {
                case SpecialMove.Castle:
                    // If the move was a castle, undo the rook's move as well
                    if (move.EndPosition.x == 2) // queenside castle
                    {
                        var rook = pieces[3, move.EndPosition.y];
                        pieces[0, move.EndPosition.y] = rook;
                        pieces[3, move.EndPosition.y] = null;
                    }
                    else if (move.EndPosition.x == 6) // kingside castle
                    {
                        var rook = pieces[5, move.EndPosition.y];
                        pieces[7, move.EndPosition.y] = rook;
                        pieces[5, move.EndPosition.y] = null;
                    }

                    // Undo the move
                    UndoMove(move);

                    break;

                case SpecialMove.EnPassant:
                    // If the move was en passant, restore the taken pawn
                    if (move.TakenPiece != null)
                    {
                        move.TakenPiece.transform.position =
                            move.OffBoardPosition.Value; // Move the taken piece back to its off-board position
                        move.TakenPiece.SetScale(Vector3.one * takeSize);
                        move.TakenPiece.gameObject.SetActive(true);
                    }

                    UndoMove(move);

                    break;


                case SpecialMove.Promotion:
                    // only proceed if there are pieces to undo the promotion of
                    if (originalPieces.Count > 0 && promotedPieces.Count > 0)
                    {
                        move.CurrentPawn = originalPieces.Pop();
                        move.CurrentPromote = promotedPieces.Pop();

                        // if the move was a promotion, downgrade the piece
                        // first, remove the promoted piece
                        move.CurrentPromote.gameObject.SetActive(false);

                        // Restore the taken piece, if there was one
                        if (move.TakenPiece != null)
                        {
                            move.TakenPiece.transform.position = move.OffBoardPosition.Value;
                            move.TakenPiece.SetScale(Vector3.one * takeSize);
                            move.TakenPiece.gameObject.SetActive(true);
                        }

                        pieces[move.StartPosition.x, move.StartPosition.y] = move.CurrentPawn; // restore the pawn
                        move.CurrentPawn.gameObject.SetActive(true);
                        pieces[move.EndPosition.x, move.EndPosition.y] = move.TakenPiece;
                        PositionSinglePiece(move.StartPosition.x, move.StartPosition.y); // position the pawn back
                        if (move.TakenPiece != null)
                        {
                            PositionSinglePiece(move.EndPosition.x, move.EndPosition.y);
                            if (move.TakenPiece.team == 0)
                                takenWhitePiece.Remove(move.TakenPiece);
                            else
                                takenBlackPiece.Remove(move.TakenPiece);
                        }
                    }

                    break;

                case SpecialMove.None: //default
                    if (move.TakenPiece != null)
                    {
                        move.TakenPiece.transform.position =
                            move.OffBoardPosition.Value; // Move the taken piece back to its off-board position
                        move.TakenPiece.SetScale(Vector3.one * takeSize);
                        move.TakenPiece.gameObject.SetActive(true);
                    }

                    UndoMove(move);

                    break;
            }

            moveIndex--;
        }

        public void MoveForward()
        {
            if (moveIndex >= moveHistory.Count - 1) return; // No move to go forward to

            // Increment the move index
            moveIndex++;

            // Get the move to redo
            var move = moveHistory[moveIndex];

            // Check the type of special move
            switch (move.SpecialMoveType)
            {
                case SpecialMove.Castle:
                    switch (move.EndPosition.x)
                    {
                        // If the move was a castle, redo the rook's move as well
                        // queenside castle
                        case 2:
                        {
                            var rook = pieces[0, move.EndPosition.y];
                            pieces[0, move.EndPosition.y] = null;
                            pieces[3, move.EndPosition.y] = rook;
                            break;
                        }
                        // kingside castle
                        case 6:
                        {
                            var rook = pieces[7, move.EndPosition.y];
                            pieces[7, move.EndPosition.y] = null;
                            pieces[5, move.EndPosition.y] = rook;
                            break;
                        }
                    }

                    break;

                case SpecialMove.EnPassant:
                    // If the move was en passant, remove the taken pawn
                    if (move.Piece.team == 0) // white team
                        pieces[move.EndPosition.x, move.EndPosition.y - 1] = null;
                    else // black team
                        pieces[move.EndPosition.x, move.EndPosition.y + 1] = null;

                    break;

                case SpecialMove.Promotion:
                    // if we have taken into promote, remove the taken piece
                    if (move.TakenPiece != null)
                    {
                        if (move.TakenPiece.team == 0) // black team
                        {
                            var offBoardPosition = new Vector3(9 * tileSize, yOffset, -1 * tileSize)
                                                   - bounds
                                                   + new Vector3(tileSize / 2, 0, tileSize / 2)
                                                   + Vector3.forward * takeSpace * takenWhitePiece.Count;
                            move.TakenPiece.SetPosition(offBoardPosition);
                            move.TakenPiece.SetScale(Vector3.one * takeSize);
                            takenWhitePiece.Add(move.TakenPiece); // update the list
                        }
                        else // white team
                        {
                            var offBoardPosition = new Vector3(-2 * tileSize, yOffset, 9 * tileSize)
                                                   - bounds
                                                   + new Vector3(tileSize / 2, 0, tileSize / 2)
                                                   + Vector3.back * takeSpace * takenBlackPiece.Count;
                            move.TakenPiece.SetPosition(offBoardPosition);
                            move.TakenPiece.SetScale(Vector3.one * takeSize);
                            takenBlackPiece.Add(move.TakenPiece); // update the list
                        }
                    }

                    move.CurrentPawn.gameObject.SetActive(false);
                    pieces[move.EndPosition.x, move.EndPosition.y] = move.CurrentPromote;
                    move.CurrentPromote.gameObject.SetActive(true);
                    PositionSinglePiece(move.EndPosition.x, move.EndPosition.y);
                    PositionSinglePiece(move.StartPosition.x, move.StartPosition.y);
                    promotedPieces.Push(move.CurrentPromote);
                    originalPieces.Push(move.CurrentPawn);

                    break;

                case SpecialMove.None:
                    // If a piece was taken in the move, return it to its off-board position
                    if (move.TakenPiece != null)
                    {
                        if (move.TakenPiece.team == 0) // black team
                        {
                            var offBoardPosition = new Vector3(9 * tileSize, yOffset, -1 * tileSize)
                                                   - bounds
                                                   + new Vector3(tileSize / 2, 0, tileSize / 2)
                                                   + Vector3.forward * takeSpace * takenWhitePiece.Count;
                            move.TakenPiece.SetPosition(offBoardPosition);
                            move.TakenPiece.SetScale(Vector3.one * takeSize);
                            takenWhitePiece.Add(move.TakenPiece); // update the list
                        }
                        else // white team
                        {
                            var offBoardPosition = new Vector3(-2 * tileSize, yOffset, 9 * tileSize)
                                                   - bounds
                                                   + new Vector3(tileSize / 2, 0, tileSize / 2)
                                                   + Vector3.back * takeSpace * takenBlackPiece.Count;
                            move.TakenPiece.SetPosition(offBoardPosition);
                            move.TakenPiece.SetScale(Vector3.one * takeSize);
                            takenBlackPiece.Add(move.TakenPiece); // update the list
                        }
                    }

                    pieces[move.EndPosition.x, move.EndPosition.y] = move.Piece;
                    PositionSinglePiece(move.EndPosition.x, move.EndPosition.y);
                    break;
                
                case null:
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public List<Vector2Int[]> GetClonedMoveList()
        {
            var clonedMoveList = new List<Vector2Int[]>(moveList);
            return clonedMoveList;
        }

        public class Move
        {
            public Piece CurrentPawn;
            public Piece CurrentPromote;
            public Vector2Int EndPosition;
            public bool isCapture;
            public Vector3? OffBoardPosition; //may be unnecessary, check later
            public Piece Piece;
            public SpecialMove? SpecialMoveType;
            public Vector2Int StartPosition;
            public Piece TakenPiece;
        }
    }
}