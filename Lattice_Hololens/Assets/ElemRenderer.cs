using UnityEngine;

public class ElemRenderer : MonoBehaviour
{
    private Mesh pointMesh;

    public void UpdateMesh(float[] vertices, byte[] colors, int pointsToRender, int pointsRendered)
    {
        int pointCount = CalculatePointCount(vertices, colors, pointsToRender, pointsRendered);
        
        if (pointCount <= 0)
        {
            ClearMesh();
            return;
        }

        var meshData = GenerateMeshData(vertices, colors, pointCount, pointsRendered);
        ApplyMeshData(meshData);
    }

    private int CalculatePointCount(float[] vertices, byte[] colors, int pointsToRender, int pointsRendered)
    {
        if (vertices == null || colors == null) return 0;
        
        int availablePoints = (vertices.Length / 3) - pointsRendered;
        int requestedPoints = System.Math.Min(pointsToRender, availablePoints);
        return System.Math.Min(requestedPoints, 65535);
    }

    private (Vector3[] points, int[] indices, Color[] colors) GenerateMeshData(float[] vertices, byte[] colors, int pointCount, int pointsRendered)
    {
        var points = new Vector3[pointCount];
        var indices = new int[pointCount];
        var pointColors = new Color[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            int vertexIndex = 3 * (pointsRendered + i);
            
            points[i] = new Vector3(
                vertices[vertexIndex], 
                vertices[vertexIndex + 1], 
                -vertices[vertexIndex + 2]
            );
            
            indices[i] = i;
            
            pointColors[i] = new Color(
                colors[vertexIndex] / 256.0f,
                colors[vertexIndex + 1] / 256.0f,
                colors[vertexIndex + 2] / 256.0f,
                1.0f
            );
        }

        return (points, indices, pointColors);
    }

    private void ApplyMeshData((Vector3[] points, int[] indices, Color[] colors) meshData)
    {
        DestroyExistingMesh();
        
        pointMesh = new Mesh();
        pointMesh.vertices = meshData.points;
        pointMesh.colors = meshData.colors;
        pointMesh.SetIndices(meshData.indices, MeshTopology.Points, 0);
        
        GetComponent<MeshFilter>().mesh = pointMesh;
    }

    private void ClearMesh()
    {
        DestroyExistingMesh();
        GetComponent<MeshFilter>().mesh = null;
    }

    private void DestroyExistingMesh()
    {
        if (pointMesh != null)
        {
            Destroy(pointMesh);
            pointMesh = null;
        }
    }
}
