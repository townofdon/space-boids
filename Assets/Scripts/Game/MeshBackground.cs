using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class MeshBackground : MonoBehaviour
{
    [SerializeField] bool debug;
    [SerializeField]
    [Tooltip("Number of vertices per square unit")]
    [Range(0.5f, 10f)]
    float vertexDensity = 5;
    [SerializeField] Gradient backgroundColor;

    List<MeshBackgroundEntity> entities = new List<MeshBackgroundEntity>();

    BoxCollider2D box;
    MeshFilter meshFilter;
    Mesh mesh;

    Vector3[] vertices;
    Color[] colors;
    Vector2[] uv;
    int[] triangles = new int[0];

    float gridWidth;
    float gridHeight;
    float vertexStepSize = 1;
    int gridSizeX = 0;
    int gridSizeY = 0;

    float previousVertexDensity;

    public void RegisterEntity(MeshBackgroundEntity incoming)
    {
        if (entities.Contains(incoming)) return;
        entities.Add(incoming);
    }

    public void DeregisterEntity(MeshBackgroundEntity outgoing)
    {
        if (!entities.Contains(outgoing)) return;
        entities.Remove(outgoing);
    }

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        box = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        CalculateMeshSizing();
        InitMesh();
    }

    private void Update()
    {
        if (vertexDensity != previousVertexDensity)
        {
            CalculateMeshSizing();
            InitMesh();
        }
        DrawMesh();
    }

    void CalculateMeshSizing()
    {
        gridWidth = box.bounds.max.x - box.bounds.min.x;
        gridHeight = box.bounds.max.y - box.bounds.min.y;
        vertexStepSize = 1f / vertexDensity;
        gridSizeX = Mathf.FloorToInt(gridWidth * vertexDensity) + 1;
        gridSizeY = Mathf.FloorToInt(gridHeight * vertexDensity) + 1;
        previousVertexDensity = vertexDensity;

        Debug.Log("=======================================");
        Debug.Log($"width={gridWidth}, height={gridHeight}");
        Debug.Log($"vertexStepSize={vertexStepSize}");
        Debug.Log($"gridSizeX={gridSizeX}, gridSizeY={gridSizeY}");
    }

    void InitMesh()
    {
        mesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        vertices = new Vector3[gridSizeX * gridSizeY];
        colors = new Color[gridSizeX * gridSizeY];
        uv = new Vector2[gridSizeX * gridSizeY];

        for (int i = 0, y = 0; y < gridSizeY; y++)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                // float z = Mathf.PerlinNoise(box.bounds.min.x + x * vertexStepSize, box.bounds.min.y + y * vertexStepSize);
                float z = 0f;
                vertices[i] = new Vector3(box.bounds.min.x + x * vertexStepSize, box.bounds.min.y + y * vertexStepSize, z);
                colors[i] = backgroundColor.Evaluate(0f);
                uv[i] = new Vector3((float)x / gridSizeX, (float)y / gridSizeY);
                i++;
            }
        }

        triangles = new int[6 * (gridSizeX - 1) * (gridSizeY - 1)];
        int vert = 0;
        int tris = 0;

        for (int y = 0; y < gridSizeY - 1; y++)
        {
            for (int x = 0; x < gridSizeX - 1; x++)
            {
                triangles[tris + 0] = vert + 0; // BL
                triangles[tris + 1] = vert + gridSizeX; // TL
                triangles[tris + 2] = vert + 1; // BR
                triangles[tris + 3] = vert + 1; // BR
                triangles[tris + 4] = vert + gridSizeX; // TL
                triangles[tris + 5] = vert + gridSizeX + 1; // TR
                vert++;
                tris += 6;
            }
            vert++;
        }
    }

    void DrawMesh()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            SetZFromEntityHistories(i);
            colors[i] = backgroundColor.Evaluate(vertices[i].z);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }

    // 0 => far, 0.5 => close, 1 => same location
    void SetZFromEntityHistories(int i)
    {
        vertices[i].z = 0f;
        foreach (var entity in entities)
        {
            for (int j = 0; j < entity.historyCount; j++)
            {
                float z = 1f / (1f - Vector2.Distance(vertices[i], entity.history[j]));
                z *= ((entity.historyCount - j) / entity.historyCount);
                vertices[i].z = Mathf.Max(vertices[i].z, z);
                if (vertices[i].z >= 1) break;
            }
        }
    }

    bool isClockwise()
    {
        // (x2 − x1)(y2 + y1)
        // see: https://stackoverflow.com/a/1165943
        float sum = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            sum += (vertices[GetNextIndex(i)].x - vertices[i].x) * (vertices[GetNextIndex(i)].y + vertices[i].y);
        }
        return sum >= 0;
    }

    int GetNextIndex(int index)
    {
        return (index + 1) % vertices.Length;
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;
        if (vertices == null || vertices.Length <= 0 || vertices[0] == null) return;
        for (int i = 0; i < vertices.Length; i++)
        {
            // Gizmos.color = new Color(uv[i].x, uv[i].y, 0f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(vertices[i], 0.05f);
        }
    }
}