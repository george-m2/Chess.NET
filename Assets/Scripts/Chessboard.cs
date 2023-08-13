using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chessboard : MonoBehaviour
{

    [Header("Graphics")] //creates a header so that the graphics (ie, visual asset) config is seperate in the inspector
    [SerializeField] public Material tileMaterial;
    [SerializeField] public float tileSize = 1.0f;
    [SerializeField] public float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;


    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8; //creates constant fall back values of grid size
    private Camera currentCamera; //init Unity Camera class which lets the player see the board 
    private GameObject[,] tiles; //Instantiates the base class for all Unity entities
    private Vector2Int currentHover;
    private Vector3 bounds;

    private void Awake()  //Grid is generated on scene load
    {
        GenerateGridTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        //Change when asset imported 
        //Creates a 1 meter grid of 8x8 units on scene start
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }
        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            //Get the indexes of tile we hit
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            //If we are hovering any tile after not hovering any tile
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            //if we were already hovernig a tile, change prewius
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[currentHover.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }
        }
    }

    //board generation
    private void GenerateGridTiles(float tileSize, int tileSizeX, int tileSizeY) //defines the size of the board, and vertical/horizontal length 
    {

        yOffset += transform.position.y;
        bounds = new Vector3((tileSizeX / 2) * tileSize, 0, (tileSizeX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileSizeX, tileSizeY];
        for(int x = 0; x < tileSizeX; x++)
        {
            for(int y = 0; y < tileSizeY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);  //nested for loop to generate a chessboard matrix
            }
        }
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)

    {
        GameObject tileObject = new GameObject($"X:{x}, Y:{y}"); //generate the tile GameObject
        tileObject.transform.parent = transform; //adds the tile GameObject to the titles GameObject

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh; //creates a reference to hold a 2D mesh
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial; //render the 2D mesh 

        Vector3[] vertices = new Vector3[4]; //generates 4 vertices for the board
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y+1) * tileSize) - bounds;   //vertex in each corner of the grid
        vertices[2] = new Vector3((x+1)* tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x+1) * tileSize, yOffset, (y+1) * tileSize) - bounds;
            
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
    //note Vector2Int, not Vector2(Float) due to use of arrays
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x,y] == hitInfo)
                    return new Vector2Int(x,y);

        return -Vector2Int.one; //OutOfBounds exception, -1,-1
    }
}

