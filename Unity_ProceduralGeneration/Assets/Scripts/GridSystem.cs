using System.Collections.Generic;
using UnityEngine;

public class GridSystem : MonoBehaviour
{
    [SerializeField] private Material _terrainMaterial;
    [SerializeField] private Material _edgeMaterial;

    [SerializeField] private float _waterLevel = .4f;
    [SerializeField] private float _scale = .1f;
    [SerializeField] private int _size = 100;

    [Header("Trees")]
    [SerializeField] private GameObject[] _treePrefabs;
    [SerializeField] private float _treeScale = .05f;
    [SerializeField] private float _treeDensity = .5f;

    [Header("Plants")]
    [SerializeField] private GameObject[] _plantPrefabs;
    [SerializeField] private float _plantScale = .05f;
    [SerializeField] private float _plantDensity = .5f;

    private Cell[,] _grid;

    private void Start()
    {
        ProcGen();
    }

    private void ProcGen()
    {
        float[,] noiseMap = new float[_size, _size];

        (float xOffset, float yOffset) = (Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                //PerlinNoise is a noise map
                float noiseValue = Mathf.PerlinNoise(x * _scale + xOffset, y * _scale + yOffset);
                noiseMap[x, y] = noiseValue;
            }
        }

        float[,] falloffMap = new float[_size, _size];

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                float xv = x / (float)_size * 2 - 1;
                float yv = y / (float)_size * 2 - 1;
                float v = Mathf.Max(Mathf.Abs(xv), Mathf.Abs(yv));
                falloffMap[x, y] = Mathf.Pow(v, 3f) / (Mathf.Pow(v, 3f) + Mathf.Pow(2.2f - 2.2f * v, 3f));
            }
        }

        _grid = new Cell[_size, _size];

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                float noiseValue = noiseMap[x, y];
                noiseValue -= falloffMap[x, y];
                bool isWater = noiseValue < _waterLevel;
                Cell cell = new Cell(isWater);
                _grid[x, y] = cell;
            }
        }

        DrawTerrainMesh(_grid);
        DrawEdgeMesh(_grid);
        DrawTexture(_grid);
        GenerateTrees(_grid);
        GeneratePlants(_grid);
    }

    private void DrawTerrainMesh(Cell[,] grid)
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                Cell cell = grid[x, y];

                if(!cell.IsWater) 
                {
                    Vector3 a = new Vector3(x - .5f, 0, y + .5f);
                    Vector3 b = new Vector3(x + .5f, 0, y + .5f);
                    Vector3 c = new Vector3(x - .5f, 0, y - .5f);
                    Vector3 d = new Vector3(x + .5f, 0, y - .5f);

                    Vector2 uvA = new Vector2(x / (float)_size, y / (float)_size);
                    Vector2 uvB = new Vector2((x + 1) / (float)_size, y / (float)_size);
                    Vector2 uvC = new Vector2(x / (float)_size, (y + 1) / (float)_size);
                    Vector2 uvD = new Vector2((x + 1) / (float)_size, (y + 1) / (float)_size);

                    Vector3[] v = new Vector3[] { a, b, c, b, d, c };
                    Vector2[] uv = new Vector2[] { uvA, uvB, uvC, uvB, uvD, uvC };

                    for (int k = 0; k < 6; k++)
                    {
                        vertices.Add(v[k]);
                        triangles.Add(triangles.Count);
                        uvs.Add(uv[k]);
                    }
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
    }

    private void DrawTexture(Cell[,] grid)
    {
        Texture2D texture = new Texture2D(_size, _size);
        Color[] colorMap = new Color[_size * _size];

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                Cell cell = grid[x, y];
                if (cell.IsWater)
                {
                    colorMap[y * _size + x] = Color.blue;
                }
                else
                {
                    colorMap[y * _size + x] = new Color (242 / 255f, 202 / 255f, 107 / 255f);
                }
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.SetPixels(colorMap);
        texture.Apply();

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.material = _terrainMaterial;
        meshRenderer.material.mainTexture = texture;
    }

    private void DrawEdgeMesh(Cell[,] grid)
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
                    if (x > 0)
                    {
                        Cell left = grid[x - 1, y];

                        if (left.IsWater)
                        {
                            Vector3 a = new Vector3(x - .5f, 0, y + .5f);
                            Vector3 b = new Vector3(x - .5f, 0, y - .5f);
                            Vector3 c = new Vector3(x - .5f, -1, y + .5f);
                            Vector3 d = new Vector3(x - .5f, -1, y - .5f);

                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };

                            for (int k = 0; k < 6; k++)
                            {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                        }
                    }

                    if (x < _size - 1)
                    {
                        Cell right = grid[x + 1, y];

                        if (right.IsWater)
                        {
                            Vector3 a = new Vector3(x + .5f, 0, y - .5f);
                            Vector3 b = new Vector3(x + .5f, 0, y + .5f);
                            Vector3 c = new Vector3(x + .5f, -1, y - .5f);
                            Vector3 d = new Vector3(x + .5f, -1, y + .5f);

                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };

                            for (int k = 0; k < 6; k++)
                            {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                        }
                    }

                    if (y > 0)
                    {
                        Cell down = grid[x, y - 1];

                        if (down.IsWater)
                        {
                            Vector3 a = new Vector3(x - .5f, 0, y - .5f);
                            Vector3 b = new Vector3(x + .5f, 0, y - .5f);
                            Vector3 c = new Vector3(x - .5f, -1, y - .5f);
                            Vector3 d = new Vector3(x + .5f, -1, y - .5f);

                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };

                            for (int k = 0; k < 6; k++)
                            {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                        }
                    }

                    if (y < _size - 1)
                    {
                        Cell up = grid[x, y + 1];
                        if (up.IsWater)
                        {
                            Vector3 a = new Vector3(x + .5f, 0, y + .5f);
                            Vector3 b = new Vector3(x - .5f, 0, y + .5f);
                            Vector3 c = new Vector3(x + .5f, -1, y + .5f);
                            Vector3 d = new Vector3(x - .5f, -1, y + .5f);

                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };

                            for (int k = 0; k < 6; k++)
                            {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                        }
                    }
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        GameObject edgeObj = new GameObject("Edge");
        edgeObj.transform.SetParent(transform);

        MeshFilter meshFilter = edgeObj.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = edgeObj.AddComponent<MeshRenderer>();
        meshRenderer.material = _edgeMaterial;
    }

    private void GenerateTrees(Cell[,] grid)
    {
        float[,] noiseMap = new float[_size, _size];

        (float xOffset, float yOffset) = (Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                //PerlinNoise is a noise map
                float noiseValue = Mathf.PerlinNoise(x * _treeScale + xOffset, y * _treeScale + yOffset);
                noiseMap[x, y] = noiseValue;
            }
        }

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                Cell cell = grid[x, y];

                if (!cell.IsWater)
                {
                    float v = Random.Range(0f, _treeDensity);

                    if (noiseMap[x, y] < v)
                    {
                        //Its a tree

                        GameObject treePrefab = _treePrefabs[Random.Range(0, _treePrefabs.Length)];
                        GameObject tree = Instantiate(treePrefab, transform);
                        tree.transform.position = new Vector3(x, 0, y);
                        tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                        tree.transform.localScale = Vector3.one * Random.Range(.8f, 1.2f);
                    }
                }
            }
        }
    }

    private void GeneratePlants(Cell[,] grid)
    {
        float[,] noiseMap = new float[_size, _size];

        (float xOffset, float yOffset) = (Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                //PerlinNoise is a noise map
                float noiseValue = Mathf.PerlinNoise(x * _plantScale + xOffset, y * _plantScale + yOffset);
                noiseMap[x, y] = noiseValue;
            }
        }

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                Cell cell = grid[x, y];

                if (!cell.IsWater)
                {
                    float v = Random.Range(0f, _plantDensity);

                    if (noiseMap[x, y] < v)
                    {
                        //Its a plant

                        GameObject plantPrefab = _plantPrefabs[Random.Range(0, _plantPrefabs.Length)];
                        GameObject plant = Instantiate(plantPrefab, transform);
                        plant.transform.position = new Vector3(x, 0, y);
                        plant.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                        plant.transform.localScale = Vector3.one * Random.Range(.8f, 1.2f);
                    }
                }
            }
        }
    }

    //private void OnDrawGizmos()
    //{
    //    if (!Application.isPlaying) { return; }

    //    for (int y = 0; y < _size; y++)
    //    {
    //        for (int x = 0; x < _size; x++)
    //        {
    //            Cell cell = _grid[x, y];

    //            if (cell.IsWater)
    //            {
    //                Gizmos.color = Color.blue;
    //            }
    //            else
    //            {
    //                Gizmos.color = Color.green;
    //            }

    //            Vector3 pos = new Vector3(x, 0, y);
    //            Gizmos.DrawCube(pos, Vector3.one);
    //        }
    //    }
    //}
}
