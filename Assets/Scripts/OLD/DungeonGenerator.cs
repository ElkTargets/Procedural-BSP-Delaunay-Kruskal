using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using Random = System.Random;
using TMPro;

namespace Old
{
    public class DungeonGenerator : MonoBehaviour
{
    public int width = 50;
    public int height = 50;
    public int minRoomSize = 5;
    public int maxRoomSize = 10;
    public Tile blackTile;
    public Tile whiteTile;
    public Tilemap tilemap;
    public int seed;
    public bool randomSeed;
    //public GameObject roomNamePrefab; // Préfab pour le texte des noms de pièces
    public GameObject linePrefab; // Préfab pour le LineRenderer

    private Random _random;
    private List<Vector2> _roomCenters = new List<Vector2>();

    private void Start() {
        if (randomSeed) { seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue); }
        _random = new Random(seed);

        GenerateDungeon();
    }

    private void GenerateDungeon() {
        bool[,] dungeonGrid = new bool[width, height];

        BSPNode rootNode = new BSPNode(0, 0, width, height);

        List<BSPNode> leafNodes = new List<BSPNode>();
        rootNode.Split(leafNodes, _random, minRoomSize);

        foreach (BSPNode node in leafNodes) {
            GenerateRoom(dungeonGrid, node);
        }

        DisplayGrid(dungeonGrid);

        foreach (BSPNode node in leafNodes)
        {
            Vector2 roomCenter = new Vector2(node.X + node.Width / 2, node.Y + node.Height / 2);
            _roomCenters.Add(roomCenter);
            Debug.Log($"Room center: {roomCenter} for node at ({node.X}, {node.Y}) with size ({node.Width}, {node.Height})");
        }

        List<Triangle> triangles = DelaunayTriangulation(_roomCenters);

        CalculateDistances(triangles);

        // Ajouter les noms des pièces
        /*for (int i = 0; i < leafNodes.Count; i++) {
            AddRoomName(leafNodes[i], i);
        }*/
        
        DrawDelaunayTriangulation(triangles);
    }

    private void GenerateRoom(bool[,] dungeonGrid, BSPNode node) {
        int roomWidth = _random.Next(minRoomSize, Math.Min(node.Width, maxRoomSize) + 1);
        int roomHeight = _random.Next(minRoomSize, Math.Min(node.Height, maxRoomSize) + 1);

        int roomX = _random.Next(node.X, node.X + node.Width - roomWidth);
        int roomY = _random.Next(node.Y, node.Y + node.Height - roomHeight);

        if (IsRoomValid(dungeonGrid, roomX, roomY, roomWidth, roomHeight))
        {
            PlaceRoom(dungeonGrid, roomX, roomY, roomWidth, roomHeight);
        }
    }

    private bool IsRoomValid(bool[,] dungeonGrid, int x, int y, int width, int height) {
        for (int i = x; i < x + width; i++) {
            for (int j = y; j < y + height; j++) {
                if (dungeonGrid[i, j]) {
                    return false;
                }
            }
        }
        return true;
    }

    private void PlaceRoom(bool[,] dungeonGrid, int x, int y, int width, int height) {
        for (int i = x; i < x + width; i++) {
            for (int j = y; j < y + height; j++) {
                dungeonGrid[i, j] = true;
            }
        }
    }

    private void DisplayGrid(bool[,] dungeonGrid)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Tile tile = dungeonGrid[x, y] ? whiteTile : blackTile;
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    private List<Triangle> DelaunayTriangulation(List<Vector2> points)
    {
        List<Triangle> triangles = new List<Triangle>();
        for (int i = 0; i < points.Count; i++) {
            for (int j = i + 1; j < points.Count; j++) {
                for (int k = j + 1; k < points.Count; k++) {
                    Vector2 p1 = points[i];
                    Vector2 p2 = points[j];
                    Vector2 p3 = points[k];

                    if (IsCounterClockwise(p1, p2, p3)) {
                        triangles.Add(new Triangle(i, j, k));
                    }
                }
            }
        }
        return triangles;
    }

    private bool IsCounterClockwise(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p2.x - p1.x) * (p3.y - p1.y) - (p2.y - p1.y) * (p3.x - p1.x) > 0;
    }

    private void CalculateDistances(List<Triangle> triangles)
    {
        foreach (Triangle tri in triangles)
        {
            Vector2 p1 = _roomCenters[tri.P0];
            Vector2 p2 = _roomCenters[tri.P1];
            Vector2 p3 = _roomCenters[tri.P2];

            float distance1 = Vector2.Distance(p1, p2);
            float distance2 = Vector2.Distance(p2, p3);
            float distance3 = Vector2.Distance(p3, p1);

            Debug.Log($"Distance entre room {tri.P0} et room {tri.P1} : {distance1}");
            Debug.Log($"Distance entre room {tri.P1} et room {tri.P2} : {distance2}");
            Debug.Log($"Distance entre room {tri.P2} et room {tri.P0} : {distance3}");
        }
    }

    /*private void AddRoomName(BSPNode node, int roomIndex) {
        Vector3 roomCenter = new Vector3(node.X + node.Width / 2, node.Y + node.Height / 2, -1.5f);
        GameObject roomNameObject = Instantiate(roomNamePrefab, roomCenter, Quaternion.identity);
        TextMeshPro roomNameText = roomNameObject.GetComponent<TextMeshPro>();
        roomNameText.text = $"Room {roomIndex}";
        roomNameText.alignment = TextAlignmentOptions.Center;
    }*/

    private void DrawDelaunayTriangulation(List<Triangle> triangles) {
        foreach (Triangle tri in triangles) {
            DrawLine(_roomCenters[tri.P0], _roomCenters[tri.P1]);
            DrawLine(_roomCenters[tri.P1], _roomCenters[tri.P2]);
            DrawLine(_roomCenters[tri.P2], _roomCenters[tri.P0]);
        }
    }

    private void DrawLine(Vector2 start, Vector2 end) {
        GameObject lineObject = Instantiate(linePrefab);
        LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}

public class Triangle {
    public int P0 { get; private set; }
    public int P1 { get; private set; }
    public int P2 { get; private set; }

    public Triangle(int p0, int p1, int p2) {
        P0 = p0;
        P1 = p1;
        P2 = p2;
    }
}

}
