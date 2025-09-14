using UnityEngine;
using System.Collections.Generic;

public class PointCloudRenderer : MonoBehaviour
{
    [Header("Rendering Configuration")]
    public int maxChunkSize = 65535;
    public float pointSize = 0.005f;
    public GameObject pointCloudElem;
    public Material pointCloudMaterial;

    private List<GameObject> renderElements;

    void Start()
    {
        renderElements = new List<GameObject>();
        RefreshPointSize();
    }

    void Update()
    {
        if (transform.hasChanged)
        {
            RefreshPointSize();
            transform.hasChanged = false;
        }
    }

    private void RefreshPointSize()
    {
        pointCloudMaterial.SetFloat("_PointSize", pointSize * transform.localScale.x);
    }

    public void Render(float[] vertices, byte[] colors)
    {
        var renderData = CalculateRenderData(vertices, colors);
        AdjustElementCount(renderData.chunkCount);
        RenderChunks(vertices, colors, renderData);
    }

    private (int pointCount, int chunkCount) CalculateRenderData(float[] vertices, byte[] colors)
    {
        if (vertices == null || colors == null)
            return (0, 0);

        int pointCount = vertices.Length / 3;
        int chunkCount = 1 + pointCount / maxChunkSize;
        return (pointCount, chunkCount);
    }

    private void AdjustElementCount(int requiredChunks)
    {
        if (renderElements.Count < requiredChunks)
            CreateAdditionalElements(requiredChunks - renderElements.Count);
        else if (renderElements.Count > requiredChunks)
            RemoveExcessElements(renderElements.Count - requiredChunks);
    }

    private void RenderChunks(float[] vertices, byte[] colors, (int pointCount, int chunkCount) renderData)
    {
        int pointOffset = 0;
        
        for (int chunkIndex = 0; chunkIndex < renderData.chunkCount; chunkIndex++)
        {
            int pointsInChunk = System.Math.Min(maxChunkSize, renderData.pointCount - pointOffset);
            
            var renderer = renderElements[chunkIndex].GetComponent<ElemRenderer>();
            renderer.UpdateMesh(vertices, colors, pointsInChunk, pointOffset);
            
            pointOffset += pointsInChunk;
        }
    }

    private void CreateAdditionalElements(int elementCount)
    {
        for (int i = 0; i < elementCount; i++)
        {
            GameObject newElement = Instantiate(pointCloudElem, transform);
            newElement.transform.localPosition = Vector3.zero;
            newElement.transform.localRotation = Quaternion.identity;
            newElement.transform.localScale = Vector3.one;
            
            renderElements.Add(newElement);
        }
    }

    private void RemoveExcessElements(int elementCount)
    {
        for (int i = 0; i < elementCount; i++)
        {
            Destroy(renderElements[0]);
            renderElements.RemoveAt(0);
        }
    }
}
