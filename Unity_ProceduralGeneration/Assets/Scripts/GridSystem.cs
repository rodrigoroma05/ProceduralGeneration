using UnityEngine;
using System.Collections.Generic;

public class GridSystem : MonoBehaviour
{
    [SerializeField] private float _waterLevel = .4f;
    [SerializeField] private float _scale = .1f;
    [SerializeField] private int _size = 100;

    private Cell[,] _grid;

    private void Start()
    {
        ProceduralGrid();
    }

    private void ProceduralGrid()
    {
        //Create noise map
        float[,] noiseMap = new float[_size, _size];
        float xOffset = Random.Range(-10000f, 10000f);
        float yOffset = Random.Range(-10000f, 10000f);

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                //NOTE: Perlin noise is a type of map noise
                float noiseValue = Mathf.PerlinNoise(x * _scale + xOffset, y * _scale + yOffset);
                noiseMap[x, y] = noiseValue;
            }
        }


        //Create an island
        float[,] falloffMap = new float[_size, _size];
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                float xv = x / (float) _size * 2 - 1;
                float yv = y / (float) _size * 2 - 1;
                float v = Mathf.Max(Mathf.Abs(xv), Mathf.Abs(yv));
                falloffMap[x, y] = Mathf.Pow(v, 3f) / (Mathf.Pow(v, 3f) + Mathf.Pow(2.2f - 2.2f * v, 3f));
            }
        }


        //Create the grid
        _grid = new Cell[_size, _size];

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                Cell cell = new Cell();
                float noiseValue = noiseMap[x, y];
                noiseValue -= falloffMap[x, y];
                cell.IsWater = noiseValue < _waterLevel;
                _grid[x, y] = cell;
            }
        }

        DrawTerrainMesh(_grid);
    }

    private void DrawTerrainMesh(Cell[,] grid)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                Cell cell = grid[x, y];

                if (!cell.IsWater)
                {
                    //Find the vertices
                    Vector3 a = new Vector3(x - .5f, 0, y + .5f);
                    Vector3 b = new Vector3(x + .5f, 0, y + .5f);
                    Vector3 c = new Vector3(x - .5f, 0, y - .5f);
                    Vector3 d = new Vector3(x + .5f, 0, y - .5f);

                    Vector3[] v = new Vector3[] { a, b, c, b, d, c };

                    for (int k = 0; k < 6; k++)
                    {
                        //Add the vertices
                        vertices.Add(v[k]);
                        triangles.Add(triangles.Count);
                    }
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
    }

    //Will check each grid and see if its land or water
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) { return; }

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                Cell cell = _grid[x, y];

                if (cell.IsWater)
                {
                    Gizmos.color = Color.blue;
                }
                else
                {
                    Gizmos.color = Color.green;
                }

                Vector3 pos = new Vector3(x, 0, y);
                Gizmos.DrawCube(pos, Vector3.one);
            }
        }
    }
}
