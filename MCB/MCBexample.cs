using UnityEngine;
using Unity.Mathematics;
using Nezix;

/// <summary>
/// Demo showcasing MarchingCubesBurst reuse across varying grid sizes, 
/// per-frame updates, non-cubic dimensions, and hull generation options.
/// </summary>
public class MCBexample : MonoBehaviour 
{
    [Header("Settings")]
    [SerializeField] private bool generateOuterHull = true;
    [SerializeField] private bool generateInnerHull = true;
    [SerializeField, Range(0.1f, 3.0f)] private float animateSpeed = 1.0f;

    private GameObject         blobsGo;
    private MeshFilter         blobsFilter;
    private Mesh               blobsMesh;
    private GameObject         cheeseGo;
    private MeshFilter         cheeseFilter;
    private Mesh               cheeseMesh;
    private MarchingCubesBurst mcb;
    private float              time = 0;

    void Start()
    {
        int3 staticGrid = new int3(20, 20, 20);
        float voxelSize = 0.06f;
        float[] density = GenerateCheese(staticGrid, voxelSize, time: 0f);

        // Static example (we'll reuse the same mcb object later too)
        mcb = new MarchingCubesBurst(density, staticGrid, Vector3.zero, voxelSize);
        mcb.computeIsoSurface(isoValue: 0.5f, true, true);

        Vector3[] verts = mcb.getVertices();
        Vector3[] norms = mcb.getNormals();
        int[]     tris  = mcb.getTriangles();

        // Translate to Unity's left-handed coords
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i].x *= -1f;
            norms[i].x *= -1f;
        }

        GameObject staticGo = new GameObject("GeneratedOnce");
        staticGo.transform.SetParent(transform, false);
        staticGo.transform.localPosition = Vector3.zero;

        MeshFilter mf = staticGo.AddComponent<MeshFilter>();
        Mesh staticMesh = new Mesh();
        
        staticMesh.vertices  = verts;
        staticMesh.normals   = norms;
        staticMesh.triangles = tris;
        
        mf.mesh = staticMesh;

        MeshRenderer mr = staticGo.AddComponent<MeshRenderer>();
        mr.material = GetMaterial(new Color(0.9f, 0.8f, 0.1f));

        // Set up 2x animated examples
        blobsGo  = CreateMeshHolder("Blobs",  new Vector3(-2.0f, 0, 0), GetMaterial(new Color(0.2f, 0.8f, 0.7f)), out blobsFilter,  out blobsMesh);
        cheeseGo = CreateMeshHolder("Cheese", new Vector3( 2.0f, 0, 0), GetMaterial(new Color(0.1f, 0.7f, 0.8f)), out cheeseFilter, out cheeseMesh);
    }

    void Update()
    {
        time += Time.deltaTime * animateSpeed;

        // Blobs with non-uniform dimensions
        int3 grid1 = new int3(24, 15, 24);
        float voxelSize1 = 0.06f;
        float[] density1 = GenerateBlobs(grid1, voxelSize1, time);

        // Reuse with new params
        mcb.Reuse(density1, grid1, Vector3.zero, voxelSize1);
        GenerateMeshForObject(blobsMesh, isoValue: 0.5f, generateOuterHull, generateInnerHull);

        // Swiss Cheese with different params on the same mcb instance
        int3 grid2 = new int3(20, 32, 12);
        float voxelSize2 = 0.08f;
        float[] density2 = GenerateCheese(grid2, voxelSize2, time);

        // Call Reuse() to set different params
        mcb.Reuse(density2, grid2, Vector3.zero, voxelSize2);
        GenerateMeshForObject(cheeseMesh, isoValue: 0.5f, generateOuterHull, generateInnerHull);
    }

    private void GenerateMeshForObject(Mesh targetMesh, float isoValue, bool generateOuter, bool generateInner)
    {
        mcb.computeIsoSurface(isoValue, generateOuter, generateInner);

        Vector3[] verts = mcb.getVertices();
        Vector3[] norms = mcb.getNormals();
        int[]     tris  = mcb.getTriangles();

        // Translate back to Unity left-handed coords
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i].x *= -1f;
            norms[i].x *= -1f;
        }

        targetMesh.Clear();
        targetMesh.vertices  = verts;
        targetMesh.normals   = norms;
        targetMesh.triangles = tris;
    }

    private float[] GenerateCheese(int3 grid, float voxelSize, float time)
    {
        float[] density = new float[grid.x * grid.y * grid.z];
        int id = 0;

        for (int x = 0; x < grid.x; x++)
        {
            for (int y = 0; y < grid.y; y++)
            {
                for (int z = 0; z < grid.z; z++)
                {
                    float fx = (x - grid.x * 0.5f) * voxelSize;
                    float fy = (y - grid.y * 0.5f) * voxelSize;
                    float fz = (z - grid.z * 0.5f) * voxelSize;

                    density[id++] = Mathf.Sin(fx * 4f + time) + Mathf.Cos(fy * 2f + time) + Mathf.Sin(fz * 4f);
                }
            }
        }
        return density;
    }

    private float[] GenerateBlobs(int3 grid, float voxelSize, float time)
    {
        float[] density = new float[grid.x * grid.y * grid.z];
        int id = 0;

        for (int x = 0; x < grid.x; x++)
        {
            for (int y = 0; y < grid.y; y++)
            {
                for (int z = 0; z < grid.z; z++)
                {
                    float fx = (x - grid.x * 0.5f) * voxelSize;
                    float fy = (y - grid.y * 0.5f) * voxelSize;
                    float fz = (z - grid.z * 0.5f) * voxelSize;

                    float distFromCenter = Mathf.Sqrt(fx * fx + fy * fy + fz * fz);
                    float holes = Mathf.Sin(fx * 8f) * Mathf.Sin(fy * 8f + time) * Mathf.Sin(fz * 8f);

                    density[id++] = (1.0f - distFromCenter) + holes * 0.4f;
                }
            }
        }
        return density;
    }

    private GameObject CreateMeshHolder(string name, Vector3 localPos, Material mat, out MeshFilter mf, out Mesh mesh)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;

        mf = go.AddComponent<MeshFilter>();
        mesh = new Mesh();
        mf.mesh = mesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = mat;

        return go;
    }

    private Material GetMaterial(Color col)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader != null ? shader : Shader.Find("Hidden/InternalErrorShader"));
        mat.color = col;
        return mat;
    }

    private void OnDestroy()
    {
        mcb?.Destroy();
    }
}