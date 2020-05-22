using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component that controls the compute shader and assigns the necessary variables
/// </summary>
public class MaskRenderer : MonoBehaviour
{
    private static List<GridCell> cells;

    /// <summary>
    /// Each cell registers itself at startup using this function
    /// I really wouldn't do it like this in a large game project but it is fine for a tutorial
    /// </summary>
    /// <param name="cell">The cell object to add to the list</param>
    public static void RegisterCell(GridCell cell)
    {
        cells.Add(cell);
    }

    //Properties

    /// <summary>
    /// The compute shader to use for rendering the mask
    /// </summary>
    [SerializeField]
    private ComputeShader computeShader = null;

    /// <summary>
    /// The size the mask should have
    /// Idealy this is a power of two
    /// </summary>
    [Range(64, 4096)]
    [SerializeField]
    private int TextureSize = 1024;

    /// <summary>
    /// The size of the hex grid in actual units
    /// This is used to scale the mask texture so it stretches across the map
    /// </summary>
    [SerializeField]
    private float MapSize = 0;

    /// <summary>
    /// Radius of a grid cell
    /// </summary>
    [SerializeField]
    private float Radius = 1.0f;

    /// <summary>
    /// Blend distance between visible and hidden area
    /// </summary>
    [SerializeField, Range(0.0f, 1.0f)]
    private float BlendDistance = 0.8f;

    private RenderTexture maskTexture;

    //Store thos properties so we can avoid string lookups in Update
    private static readonly int textureSizeId = Shader.PropertyToID("_TextureSize");
    private static readonly int cellCountId = Shader.PropertyToID("_CellCount");
    private static readonly int mapSizeId = Shader.PropertyToID("_MapSize");

    private static readonly int radiusId = Shader.PropertyToID("_Radius");
    private static readonly int blendId = Shader.PropertyToID("_Blend");

    private static readonly int maskTextureId = Shader.PropertyToID("_Mask");

    private static readonly int cellBufferId = Shader.PropertyToID("_CellBuffer");

    //This is the struct we parse to the compute shader for each cell
    private struct CellBufferElement
    {
        public float PositionX;
        public float PositionY;
        public float Visibility;
    }

    private List<CellBufferElement> bufferElements;
    private ComputeBuffer buffer = null;

    /// <summary>
    /// Initialization
    /// </summary>
    private void Awake()
    {
        //It is important that this is in Awake and the Cell's are getting added in Start()
        cells = new List<GridCell>();

        //Create a new render texture for the mask
        maskTexture = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.ARGB32) 
        { 
            enableRandomWrite = true 
        };
        maskTexture.Create();

        //Set the texture dimension and the mask texture in the compute shader
        computeShader.SetInt(textureSizeId, TextureSize);
        computeShader.SetTexture(0, maskTextureId, maskTexture);

        //We are using the mask texture and the map size in multiple materials
        //Setting it as a global variable is easier in this case
        Shader.SetGlobalTexture(maskTextureId, maskTexture);
        Shader.SetGlobalFloat(mapSizeId, MapSize);

        bufferElements = new List<CellBufferElement>();
    }

    private void OnDestroy()
    {
        buffer?.Dispose();

        if (maskTexture != null)
            DestroyImmediate(maskTexture);
    }

    //Setup all buffers and variables
    private void Update()
    {
        //Recreate the buffer since the visibility updates
        //This is not extremely optimized as we could also simply change 
        //values but it is fine for a project as small as this one
        bufferElements.Clear();
        foreach (GridCell cell in cells)
        {
            CellBufferElement element = new CellBufferElement
            {
                PositionX = cell.transform.position.x,
                PositionY = cell.transform.position.z,
                Visibility = cell.Visibility
            };

            bufferElements.Add(element);
        }

        if(buffer == null)
            buffer = new ComputeBuffer(bufferElements.Count * 3, sizeof(float));

        //Set the buffer data and parse it to the compute shader
        buffer.SetData(bufferElements);
        computeShader.SetBuffer(0, cellBufferId, buffer);

        //Set other variables needed in the compute function
        computeShader.SetInt(cellCountId, bufferElements.Count);
        computeShader.SetFloat(radiusId, Radius / MapSize);
        computeShader.SetFloat(blendId, BlendDistance / MapSize);

        //Execute the compute shader
        //Our thread group size is 8x8=64, 
        //thus we have to dispatch (TextureSize / 8) * (TextureSize / 8) thread groups
        computeShader.Dispatch(0, TextureSize / 8, TextureSize / 8, 1);
    }
}
