using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

namespace SarRP.Noise
{
    public class Noise3D : MonoBehaviour
    {
        public bool UpdatePerFrame = false;
        public RenderTexture Output;
        public ComputeShader ComputeShader;
        [Delayed]
        public float Scale = 64;
        [Delayed]
        public int FBMIteration = 4;

        ComputeBuffer PermuteBuffer;
        private void Awake()
        {
            RenderNoise();
        }
        // Use this for initialization
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }
        [EditorButton]
        public void RenderNoise()
        {
            if(PermuteBuffer is null)
            {
                PermuteBuffer = new ComputeBuffer(256, sizeof(int));
                PermuteBuffer.SetData(permute);
            }
            if(!Output.enableRandomWrite)
            {
                Output.Release();
                Output.enableRandomWrite = true;
                Output.Create();
            }
            ComputeShader.SetTexture(0, Shader.PropertyToID("OutputTexture"), Output);
            ComputeShader.SetFloat(Shader.PropertyToID("Scale"), Scale);
            ComputeShader.SetVector("OutputSize", new Vector3(Output.width, Output.height, Output.volumeDepth));
            ComputeShader.SetFloat("Seed", Random.value);
            ComputeShader.SetInt(Shader.PropertyToID("Iteration"), FBMIteration);
            ComputeShader.SetBuffer(0, "perm", PermuteBuffer);
            ComputeShader.Dispatch(0, Output.width / 8, Output.height / 8, Output.volumeDepth / 8);
        }

        int[] permute = new int[] { 151,160,137,91,90,15,
   131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
   190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
   88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
   77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
   102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
   135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
   5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
   223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
   129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
   251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
   49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
   138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
   };
    }

}