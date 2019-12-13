using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Noise
{
    [ExecuteInEditMode]
    public class CurlNoiseMotion2D : MonoBehaviour
    {
        public ComputeShader MotionComputeShader;
        public RenderTexture CurlNoise;
        public bool DynamicUpdate = false;
        public float Speed = 1;
        int currentIdx;
        public RenderTexture CurrentMotionTexture => MotionTextures[currentIdx % 2];
        public RenderTexture PreviousMotionTexture => MotionTextures[(currentIdx + 1) % 2];
        public bool Debug = false;

        RenderTexture[] MotionTextures;
        [EditorButton("Reload")]
        private void Awake()
        {
            if (!CurlNoise || !MotionComputeShader)
                return;

            MotionTextures = new RenderTexture[2]
            {
                new RenderTexture(CurlNoise.width, CurlNoise.height, 0, RenderTextureFormat.RGFloat),
                new RenderTexture(CurlNoise.width, CurlNoise.height, 0, RenderTextureFormat.RGFloat),
            };
            MotionTextures[0].enableRandomWrite = true;
            MotionTextures[1].enableRandomWrite = true;
            MotionTextures[0].wrapMode = TextureWrapMode.Repeat;
            MotionTextures[1].wrapMode = TextureWrapMode.Repeat;
            MotionTextures[0].Create();
            MotionTextures[1].Create();

            MotionComputeShader.SetVector("TextureSize", new Vector2(CurlNoise.width, CurlNoise.height));
            MotionComputeShader.SetTexture(1, "CurrentMotion", PreviousMotionTexture);
            MotionComputeShader.SetTexture(1, "NextMotion", CurrentMotionTexture);
            MotionComputeShader.Dispatch(1, CurlNoise.width / 8, CurlNoise.width / 8, 1);
        }
        private void Update()
        {
            if (DynamicUpdate)
                UpdateMotion();
        }
        [EditorButton]
        public void UpdateMotion()
        {
            if (!CurlNoise || !MotionComputeShader)
                return;
            currentIdx++;
            MotionComputeShader.SetFloat("Speed", Speed);
            MotionComputeShader.SetFloat("DeltaTime", Time.deltaTime);
            MotionComputeShader.SetVector("TextureSize", new Vector2(CurlNoise.width, CurlNoise.height));
            MotionComputeShader.SetTexture(0, "CurrentMotion", PreviousMotionTexture);
            MotionComputeShader.SetTexture(0, "NextMotion", CurrentMotionTexture);
            MotionComputeShader.SetTexture(0, "CurlNoise", CurlNoise);
            MotionComputeShader.Dispatch(0, CurlNoise.width / 8, CurlNoise.height / 8, 1);
        }

        private void OnGUI()
        {
            if (Debug)
            {
                GUI.DrawTexture(new Rect(0, 0, 1024, 1024), CurrentMotionTexture, ScaleMode.ScaleToFit, false);
            }
        }
    }
}
