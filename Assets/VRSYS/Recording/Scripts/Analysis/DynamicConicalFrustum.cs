using UnityEngine;

namespace VRSYS.Recording.Scripts.Analysis
{
    
    // see: https://stackoverflow.com/a/65479858 
public class DynamicConicalFrustum : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshCollider meshCollider;

    [Header("Settings")]
    [SerializeField] private float _height = 1f;
    [SerializeField] private float _bottomRadius = .25f;
    [SerializeField] private float _topRadius = .05f;
    [SerializeField] private int nbSides = 18;

    private Mesh mesh;
    const float _2pi = Mathf.PI * 2f;
    private Vector3[] vertices;

    private void Awake()
    {
        if (!meshFilter && !TryGetComponent<MeshFilter>(out meshFilter))
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if(!GetComponent<MeshRenderer>())
        {
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Standard"));
        }

        if (!meshCollider)
            meshCollider = GetComponent<MeshCollider>();

        mesh = meshFilter.mesh;

        if (!mesh)
        {
            mesh = new Mesh();
        }

        meshFilter.mesh = mesh;
        if (meshCollider)
            meshCollider.sharedMesh = mesh;

        RecreateFrustum(_height,_bottomRadius,_topRadius);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            Awake();
        }
    }
#endif

    public void RecreateFrustum(float height, float bottomRadius, float topRadius)
    {
        mesh.Clear();

        int nbVerticesCap = nbSides + 1;
        #region Vertices

        // bottom + top + sides
        vertices = new Vector3[nbVerticesCap + nbVerticesCap + nbSides  * 2 + 2];

        // Bottom cap
        vertices[0] = new Vector3(0f, 0f, 0f);
        for(var idx = 1; idx <= nbSides; idx++)
        {
            float rad = (float)(idx ) / nbSides * _2pi;
            vertices[idx] = new Vector3(Mathf.Cos(rad) * bottomRadius, 0f, Mathf.Sin(rad) * bottomRadius);
        }

        // Top cap
        vertices[nbSides + 1] = new Vector3(0f, height, 0f);
        for(var idx = nbSides + 2; idx <= nbSides * 2 + 1; idx++)
        { 
            float rad = (float)(idx - nbSides - 1) / nbSides * _2pi;
            vertices[idx] = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
        }

        // Sides
        int v = 0;
        for(var idx = nbSides * 2 + 2; idx <= vertices.Length - 4; idx+=2)
        { 
            float rad = (float)v / nbSides * _2pi;
            vertices[idx] = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
            vertices[idx + 1] = new Vector3(Mathf.Cos(rad) * bottomRadius, 0, Mathf.Sin(rad) * bottomRadius);
            v++;
        }
        vertices[vertices.Length - 2] = vertices[nbSides * 2 + 2];
        vertices[vertices.Length - 1] = vertices[nbSides * 2 + 3];
        #endregion

        #region Triangles
        int nbTriangles = nbSides + nbSides + nbSides * 2;
        int[] triangles = new int[nbTriangles * 3 + 3];

        // Bottom cap
        int tri = 0;
        int i = 0;
        while (tri < nbSides - 1)
        {
            triangles[i] = 0;
            triangles[i + 1] = tri + 1;
            triangles[i + 2] = tri + 2;
            tri++;
            i += 3;
        }
        triangles[i] = 0;
        triangles[i + 1] = tri + 1;
        triangles[i + 2] = 1;
        tri++;
        i += 3;

        // Top cap
        //tri++;
        while (tri < nbSides * 2)
        {
            triangles[i] = tri + 2;
            triangles[i + 1] = tri + 1;
            triangles[i + 2] = nbVerticesCap;
            tri++;
            i += 3;
        }

        triangles[i] = nbVerticesCap + 1;
        triangles[i + 1] = tri + 1;
        triangles[i + 2] = nbVerticesCap;
        tri++;
        i += 3;
        tri++;

        // Sides
        while (tri <= nbTriangles)
        {
            triangles[i] = tri + 2;
            triangles[i + 1] = tri + 1;
            triangles[i + 2] = tri + 0;
            tri++;
            i += 3;

            triangles[i] = tri + 1;
            triangles[i + 1] = tri + 2;
            triangles[i + 2] = tri + 0;
            tri++;
            i += 3;
        }
        #endregion

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.Optimize();
    }
}
}