using UnityEngine;

public class WarehouseMapGenerator : MonoBehaviour
{
    [Header("Map")]
    public Vector2 mapSize = new Vector2(100f, 100f);
    public float cellSize = 0.5f;

    [Header("Flight")]
    public float flightAltitude = 2.0f;
    public float scanHeight = 0.5f;

    [Header("Safety")]
    public float safetyMargin = 0.5f;

    [Header("Detection")]
    public LayerMask obstacleMask;

    [Header("Debug")]
    public bool drawFreeCells = true;

    private bool[,] occupiedGrid;
    private int gridWidth;
    private int gridHeight;

    public bool[,] OccupiedGrid => occupiedGrid;
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;

    public Vector3 MapOrigin =>
        transform.position -
        new Vector3(mapSize.x / 2f, 0f, mapSize.y / 2f);

    void Start()
    {
        GenerateMap();
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        gridWidth = Mathf.CeilToInt(mapSize.x / cellSize);
        gridHeight = Mathf.CeilToInt(mapSize.y / cellSize);

        occupiedGrid = new bool[gridWidth, gridHeight];

        Vector3 origin = MapOrigin;

        // Detect actual static obstacles
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 cellCenter = new Vector3(
                    origin.x + (x + 0.5f) * cellSize,
                    transform.position.y + flightAltitude,
                    origin.z + (z + 0.5f) * cellSize
                );

                Vector3 halfExtents = new Vector3(
                    cellSize * 0.45f,
                    scanHeight / 2f,
                    cellSize * 0.45f
                );

                bool occupied = Physics.CheckBox(
                    cellCenter,
                    halfExtents,
                    Quaternion.identity,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore
                );

                occupiedGrid[x, z] = occupied;
            }
        }

        // Add safety clearance around obstacles
        InflateObstacles();

        // Count final occupied cells after inflation
        int occupiedCount = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (occupiedGrid[x, z])
                    occupiedCount++;
            }
        }

        Debug.Log(
            $"Map generated: {gridWidth} x {gridHeight}, " +
            $"occupied after safety margin: {occupiedCount}"
        );
    }

    private void InflateObstacles()
    {
        if (safetyMargin <= 0f)
            return;

        int radiusCells =
            Mathf.CeilToInt(safetyMargin / cellSize);

        bool[,] original =
            (bool[,])occupiedGrid.Clone();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (!original[x, z])
                    continue;

                for (int dx = -radiusCells;
                     dx <= radiusCells;
                     dx++)
                {
                    for (int dz = -radiusCells;
                         dz <= radiusCells;
                         dz++)
                    {
                        // Circular safety region
                        if (dx * dx + dz * dz >
                            radiusCells * radiusCells)
                            continue;

                        int nx = x + dx;
                        int nz = z + dz;

                        if (nx >= 0 && nx < gridWidth &&
                            nz >= 0 && nz < gridHeight)
                        {
                            occupiedGrid[nx, nz] = true;
                        }
                    }
                }
            }
        }
    }

    public Vector3 GridToWorld(int x, int z)
    {
        Vector3 origin = MapOrigin;

        return new Vector3(
            origin.x + (x + 0.5f) * cellSize,
            transform.position.y + flightAltitude,
            origin.z + (z + 0.5f) * cellSize
        );
    }

    public bool WorldToGrid(
        Vector3 worldPosition,
        out int x,
        out int z)
    {
        Vector3 origin = MapOrigin;

        x = Mathf.FloorToInt(
            (worldPosition.x - origin.x) / cellSize
        );

        z = Mathf.FloorToInt(
            (worldPosition.z - origin.z) / cellSize
        );

        return x >= 0 &&
               x < gridWidth &&
               z >= 0 &&
               z < gridHeight;
    }

    public bool IsOccupied(int x, int z)
    {
        if (occupiedGrid == null)
            return true;

        if (x < 0 || x >= gridWidth ||
            z < 0 || z >= gridHeight)
            return true;

        return occupiedGrid[x, z];
    }

    void OnDrawGizmos()
    {
        // Yellow map boundary
        Gizmos.color = Color.yellow;

        Vector3 center = new Vector3(
            transform.position.x,
            transform.position.y + flightAltitude,
            transform.position.z
        );

        Gizmos.DrawWireCube(
            center,
            new Vector3(
                mapSize.x,
                scanHeight,
                mapSize.y
            )
        );

        if (occupiedGrid == null)
            return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                bool occupied =
                    occupiedGrid[x, z];

                if (!occupied && !drawFreeCells)
                    continue;

                Gizmos.color = occupied
                    ? new Color(1f, 0f, 0f, 0.6f)
                    : new Color(0f, 1f, 0f, 0.12f);

                Gizmos.DrawCube(
                    GridToWorld(x, z),
                    new Vector3(
                        cellSize * 0.9f,
                        0.05f,
                        cellSize * 0.9f
                    )
                );
            }
        }
    }
}
