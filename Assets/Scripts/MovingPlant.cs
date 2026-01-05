using UnityEngine;

public class MovingPlant : MonoBehaviour
{
    public float swingSpeed = 1f;
    public float swingAmount = 0.3f;

    private Vector3 originalPosition;
    private MeshFilter meshFilter;
    private Vector3[] originalVertices;

    void Start()
    {
        originalPosition = transform.position;
        meshFilter = GetComponent<MeshFilter>();
        originalVertices = meshFilter.mesh.vertices;
    }

    void Update()
    {
        Vector3[] vertices = new Vector3[originalVertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];

            // 根據高度產生不同的擺動
            float swing = Mathf.Sin(Time.time * swingSpeed + vertex.y * 2f)
                        * swingAmount * vertex.y;

            vertex.x += swing;
            vertex.z += Mathf.Cos(Time.time * swingSpeed * 0.7f + vertex.y * 2f)
                      * swingAmount * 0.5f * vertex.y;

            vertices[i] = vertex;
        }

        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateNormals();
    }
}
