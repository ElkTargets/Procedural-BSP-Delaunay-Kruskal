using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapManager : MonoBehaviour {
    [SerializeField] private Vector2Int size;
    [SerializeField] private int seed;
    private System.Random _random;
    public bool randomized;
    private List<Room> _rooms = new List<Room>();
    public int depth = 3;
    public bool cutHorizontaly;

    //private Dictionary<Vector2Int, List<Vector2Int>> _connections = new Dictionary<Vector2Int, List<Vector2Int>>();

    [ContextMenu("Launch")]
    public void Launch() {
        if (randomized) { seed = Random.Range(int.MinValue, int.MaxValue); }
        _random = new System.Random(seed);
        _rooms.Clear();
        Room startRoom = new Room(Vector2Int.zero, size);
        Debug.Log(startRoom.ToString());
        List<Room> newRooms = new List<Room> { startRoom };
        newRooms = RecursiveSplit(newRooms, depth);
        List<Vector2Int> centers = new List<Vector2Int>();
        foreach (Room room in newRooms) {
            Debug.Log(room.ToString());
            Vector2Int center = GetRoomCenter(room);
            centers.Add(center);
            _rooms.Add(room);
        }

        // Perform Delaunay triangulation
        //
        /*var triangles = DelaunayTriangulation(centers);

        // Store connections in dictionary
        foreach (var triangle in triangles) {
            AddConnection(triangle.Item1, triangle.Item2);
            AddConnection(triangle.Item2, triangle.Item3);
            AddConnection(triangle.Item3, triangle.Item1);
        }

        // Log connections
        foreach (var connection in _connections) {
            Debug.Log($"Connections for {connection.Key}: {string.Join(", ", connection.Value)}");
        }*/
    }

    private List<Room> RecursiveSplit(List<Room> rooms, int depth) {
        if (depth == 0) return rooms;
        Room roomToCut = GetLargestRoom(rooms);
        (Room, Room) tuple = Split(roomToCut);
        rooms.Remove(roomToCut);
        rooms.Add(tuple.Item1);
        rooms.Add(tuple.Item2);
        return RecursiveSplit(rooms, depth - 1);
    }

    private (Room, Room) Split(Room roomToCut) {
        cutHorizontaly = !cutHorizontaly;
        float cutPercent = 0.25f + (float)_random.NextDouble() * 0.50f;
        int cutValue;
        if (cutHorizontaly) {
            cutValue = Mathf.RoundToInt(roomToCut.Size.x * cutPercent);
            Room firstRoom = new Room(roomToCut.Position, new Vector2Int(cutValue, roomToCut.Size.y));
            Room secondRoom = new Room(new Vector2Int(roomToCut.Position.x + cutValue, roomToCut.Position.y), new Vector2Int(roomToCut.Size.x - cutValue, roomToCut.Size.y));
            return (firstRoom, secondRoom);
        } else {
            cutValue = Mathf.RoundToInt(roomToCut.Size.y * cutPercent);
            Room firstRoom = new Room(roomToCut.Position, new Vector2Int(roomToCut.Size.x, cutValue));
            Room secondRoom = new Room(new Vector2Int(roomToCut.Position.x, roomToCut.Position.y + cutValue), new Vector2Int(roomToCut.Size.x, roomToCut.Size.y - cutValue));
            return (firstRoom, secondRoom);
        }
    }

    private Room GetLargestRoom(List<Room> rooms) {
        Room largestRoom = rooms[0];
        foreach (Room room in rooms) {
            if (room.Size.x * room.Size.y > largestRoom.Size.x * largestRoom.Size.y) {
                largestRoom = room;
            }
        }
        return largestRoom;
    }

    private Vector2Int GetRoomCenter(Room room)
    {
        Vector2Int center = room.CalculateCenter();
        Debug.Log($"Center of the room: x = {center.x}, y = {center.y}");
        return center;
    }

    /*private void AddConnection(Vector2Int p1, Vector2Int p2) {
        if (!_connections.ContainsKey(p1)) {
            _connections[p1] = new List<Vector2Int>();
        }
        if (!_connections.ContainsKey(p2)) {
            _connections[p2] = new List<Vector2Int>();
        }
        if (!_connections[p1].Contains(p2)) {
            _connections[p1].Add(p2);
        }
        if (!_connections[p2].Contains(p1)) {
            _connections[p2].Add(p1);
        }
        Debug.Log($"Added connection between {p1} and {p2}");
    }

    private List<(Vector2Int, Vector2Int, Vector2Int)> DelaunayTriangulation(List<Vector2Int> points) {
        List<(Vector2Int, Vector2Int, Vector2Int)> triangles = new List<(Vector2Int, Vector2Int, Vector2Int)>();

        // Create a super triangle that encompasses all points
        Vector2Int superA = new Vector2Int(0, 0);
        Vector2Int superB = new Vector2Int(1000, 0);
        Vector2Int superC = new Vector2Int(500, 1000);
        triangles.Add((superA, superB, superC));

        foreach (var point in points) {
            List<(Vector2Int, Vector2Int, Vector2Int)> badTriangles = new List<(Vector2Int, Vector2Int, Vector2Int)>();
            List<(Vector2Int, Vector2Int)> edges = new List<(Vector2Int, Vector2Int)>();

            // Find all triangles that are no longer valid due to the insertion of the new point
            foreach (var triangle in triangles) {
                if (IsPointInCircumcircle(point, triangle.Item1, triangle.Item2, triangle.Item3)) {
                    badTriangles.Add(triangle);
                }
            }

            // Find the edges of the polygon formed by the bad triangles
            foreach (var triangle in badTriangles) {
                edges.Add((triangle.Item1, triangle.Item2));
                edges.Add((triangle.Item2, triangle.Item3));
                edges.Add((triangle.Item3, triangle.Item1));
            }

            // Remove duplicate edges
            for (int i = 0; i < edges.Count; i++) {
                for (int j = i + 1; j < edges.Count; j++) {
                    if (edges[i] == edges[j] || edges[i] == (edges[j].Item2, edges[j].Item1)) {
                        edges.RemoveAt(j);
                        j--;
                    }
                }
            }

            // Remove bad triangles from the list
            foreach (var triangle in badTriangles) {
                triangles.Remove(triangle);
            }

            // Add new triangles formed by the new point and the edges of the polygon
            foreach (var edge in edges) {
                triangles.Add((edge.Item1, edge.Item2, point));
            }
        }

        // Remove triangles that contain the super triangle vertices
        triangles.RemoveAll(t => t.Item1 == superA || t.Item1 == superB || t.Item1 == superC ||
                                 t.Item2 == superA || t.Item2 == superB || t.Item2 == superC ||
                                 t.Item3 == superA || t.Item3 == superB || t.Item3 == superC);

        return triangles;
    }

    private bool IsPointInCircumcircle(Vector2Int point, Vector2Int a, Vector2Int b, Vector2Int c) {
        float ax = a.x, ay = a.y;
        float bx = b.x, by = b.y;
        float cx = c.x, cy = c.y;
        float px = point.x, py = point.y;

        float ax_sq = ax * ax, ay_sq = ay * ay;
        float bx_sq = bx * bx, by_sq = by * by;
        float cx_sq = cx * cx, cy_sq = cy * cy;
        float px_sq = px * px, py_sq = py * py;

        float det = (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by)) * 2.0f;
        float a_sq = ax_sq + ay_sq;
        float b_sq = bx_sq + by_sq;
        float c_sq = cx_sq + cy_sq;
        float p_sq = px_sq + py_sq;

        float a_b = (ax * bx + ay * by) * 2.0f;
        float a_c = (ax * cx + ay * cy) * 2.0f;
        float b_c = (bx * cx + by * cy) * 2.0f;
        float a_p = (ax * px + ay * py) * 2.0f;
        float b_p = (bx * px + by * py) * 2.0f;
        float c_p = (cx * px + cy * py) * 2.0f;

        float a_b_c = a_sq * (by - cy) + b_sq * (cy - ay) + c_sq * (ay - by) +
                      (by * cy * (ay - by) + by * ay * (cy - ay) + cy * ay * (by - cy));
        float a_b_p = a_sq * (by - py) + b_sq * (py - ay) + p_sq * (ay - by) +
                      (by * py * (ay - by) + by * ay * (py - ay) + py * ay * (by - py));
        float a_c_p = a_sq * (cy - py) + c_sq * (py - ay) + p_sq * (ay - cy) +
                      (cy * py * (ay - cy) + cy * ay * (py - ay) + py * ay * (cy - py));
        float b_c_p = b_sq * (cy - py) + c_sq * (py - by) + p_sq * (by - cy) +
                      (cy * py * (by - cy) + cy * by * (py - by) + py * by * (cy - py));

        return (a_b_c * (a_b_p + a_c_p + b_c_p) + det * (a_p + b_p + c_p)) > 0;
    }*/

    private void OnDrawGizmos() {
        foreach (Room room in _rooms) {
            room.DrawRoom();
            room.DrawCenter();
        }

        /*foreach (var connection in _connections) {
            foreach (var neighbor in connection.Value) {
                Debug.DrawLine(new Vector3(connection.Key.x, connection.Key.y, 0), new Vector3(neighbor.x, neighbor.y, 0), Color.green);
            }
        }*/
    }
}

public struct Room {
    public Vector2Int Position;
    public Vector2Int Size;

    public Room(Vector2Int position, Vector2Int size) {
        Position = position;
        Size = size;
    }

    public override string ToString() {
        return $"Position x : {Position.x}, y : {Position.y} \n " +
               $"Size x : {Size.x}, y : {Size.y}";
    }

    public Vector2Int CalculateCenter()
    {
        int centerX = Position.x + Size.x / 2;
        int centerY = Position.y + Size.y / 2;
        return new Vector2Int(centerX, centerY);
    }

    public void DrawRoom() {
        Vector3 start = new Vector3(Position.x, Position.y,0);
        Vector3 endX = new Vector3(Position.x + Size.x, Position.y,0);
        Vector3 endY = new Vector3(Position.x, Position.y + Size.y,0);
        Vector3 end = new Vector3(Position.x + Size.x, Position.y + Size.y,0);

        Debug.DrawLine(start, endX, Color.red);
        Debug.DrawLine(start, endY, Color.red);
        Debug.DrawLine(endX, end, Color.red);
        Debug.DrawLine(endY, end, Color.red);
    }

    public void DrawCenter() {
        Vector2Int center = CalculateCenter();
        Vector3 centerPosition = new Vector3(center.x, center.y, 0);
        float radius = 0.5f;
        int segments = 30;

        Vector3 prevPoint = centerPosition + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++) {
            float angle = i * (2.0f * Mathf.PI / segments);
            Vector3 nextPoint = centerPosition + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Debug.DrawLine(prevPoint, nextPoint, Color.blue);
            prevPoint = nextPoint;
        }
    }
}
