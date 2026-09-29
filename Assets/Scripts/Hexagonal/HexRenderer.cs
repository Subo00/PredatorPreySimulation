using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public struct Face
{
    public List<Vector3> vertices { get; private set; }
    public List<int> triangles { get; private set; }
    public List<Vector2> uvs { get; private set; } 

    public Face(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.uvs = uvs; 
    }
}




[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HexRenderer : MonoBehaviour
{

    protected Mesh mesh;
    protected MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;
    protected int x, y;

    protected List<Face> faces;
    protected Color defaultColor;
    protected Color foodColor = Color.white;

    public Material material;
    public float innerSize;
    public float outerSize;
    public float height;
    public bool isFlatTopped;


    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh();
        mesh.name = "Hex";

        meshFilter.mesh = mesh;
        meshRenderer.material = material;   
    }

    private void OnEnable()
    {
        DrawMesh();
    }

    public void SetCoordinates(int x, int y) { this.x = x; this.y = y; }    
    public void SetMaterial(Material material)
    {
        this.material = material;
        defaultColor = material.color;
        foodColor = defaultColor;
    }

    public void DrawMesh()
    {
        meshRenderer.material = material;
        DrawFaces();
        CombineFaces();
    }

    private void DrawFaces()
    {
        faces = new List<Face>();

        for (int point = 0; point < 6; point++)
        {
            faces.Add(CreateFace(innerSize, outerSize, height / 2f, height / 2f, point));
        }
    }

    private void CombineFaces()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();   

        for(int i = 0; i < faces.Count; i++)
        {
            vertices.AddRange(faces[i].vertices);
            uvs.AddRange(faces[i].uvs);

            int offset = (4 * i);
            foreach(int triangle in faces[i].triangles)
            {
                tris.Add(triangle + offset);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals(); //I have no clue what any of this does lol

    }


    private Face CreateFace(float innerRad, float outerRad, float heightA, float heightB, int point, bool reverse = false)
    {
        Vector3 pointA = GetPoint(innerRad, heightB, point);
        Vector3 pointB = GetPoint(innerRad, heightB, (point < 5) ? point + 1 : 0);
        Vector3 pointC = GetPoint(outerRad, heightA, (point < 5) ? point + 1 : 0);
        Vector3 pointD = GetPoint(outerRad, heightA, point);

        List<Vector3> vertices = new List<Vector3>() { pointA, pointB, pointC, pointD };
        List<int> triangles = new List<int>() { 0, 1, 2, 2, 3, 0 };
        List<Vector2> uvs = new List<Vector2>() { new Vector2(0, 0), new Vector2(1, 0),
                            new Vector2(1,1), new Vector2(0,1)};
        if(reverse)
        {
            vertices.Reverse();
        }

        return new Face(vertices, triangles, uvs);

    }

    protected Vector3 GetPoint(float size, float height, int index)
    {
        float angleDeg = isFlatTopped ? 60 * index : 60 * index - 30;
        float angleRad = Mathf.PI / 180 * angleDeg;
        return new Vector3((size * Mathf.Cos(angleRad)), height, size * Mathf.Sin(angleRad));
    }

   
}
