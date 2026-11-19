using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapBorders : MonoBehaviour
{
    public Color borderColor = Color.white;
    public float borderWidth = 0.02f;

    private Tilemap tm;
    private Material borderMat;

    void Awake()
    {
        tm = GetComponent<Tilemap>();

        borderMat = new Material(Shader.Find("Sprites/Default"));
        borderMat.color = borderColor;
    }

    // Construct lines via OpenGL
    void OnRenderObject()
    {
        borderMat.SetPass(0);

        GL.PushMatrix();
        GL.Begin(GL.LINES);
        GL.Color(borderColor);

        foreach(var pos in tm.cellBounds.allPositionsWithin)
        {
            if(!tm.HasTile(pos))
                    continue;

            Vector3 worldPos = tm.CellToWorld(pos);
            Vector3 gridSize = tm.cellSize;

            Vector3 point1 = worldPos;
            Vector3 point2 = worldPos + new Vector3(gridSize.x, 0, 0);
            Vector3 point3 = worldPos + new Vector3(gridSize.x, gridSize.y, 0);
            Vector3 point4 = worldPos + new Vector3(0, gridSize.y, 0);

            DrawLineOnGrid(point1, point2);
            DrawLineOnGrid(point2, point3);
            DrawLineOnGrid(point3, point4);
            DrawLineOnGrid(point4, point1); 
        }

        GL.End();
        GL.PopMatrix();
    }

    void DrawLineOnGrid(Vector3 _a, Vector3 _b)
    {
        GL.Vertex(_a);
        GL.Vertex(_b);
    }
}
