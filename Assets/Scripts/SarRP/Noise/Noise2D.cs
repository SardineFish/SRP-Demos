using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Noise
{
    [ExecuteInEditMode]
    public class Noise2D : MonoBehaviour
    {
        public bool UpdatePerFrame = false;
        public RenderTexture Output;
        public ComputeShader ComputeShader;
        [Delayed]
        public float Scale = 64;
        [Delayed]
        public int FBMIteration = 4;
        public bool Debug = false;

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
            if (!Output.enableRandomWrite)
            {
                Output.Release();
                Output.enableRandomWrite = true;
                Output.Create();
            }
            ComputeShader.SetTexture(0, Shader.PropertyToID("OutputTexture"), Output);
            ComputeShader.SetFloat("Scale", Scale);
            ComputeShader.SetVector("OutputSize", new Vector2(Output.width, Output.height));
            ComputeShader.SetFloat("Seed", Random.value);
            ComputeShader.SetInt("Iteration", FBMIteration);
            ComputeShader.Dispatch(0, Output.width / 8, Output.height / 8, 1);
        }

        private void OnGUI()
        {
            if(Debug)
            {
                GUI.DrawTexture(new Rect(0, 0, 1024, 1024), Output, ScaleMode.ScaleToFit, false);
            }
        }
    }
}
