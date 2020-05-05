using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;
using SarRP.Renderer;

namespace SarRP
{
    [CreateAssetMenu(fileName ="SarRenderPipeline", menuName = "SarRP/SarRenderPipeline")]
    public class SardineRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField]
        [Header("Max Shadow Distance")]
        float m_MaxShadowDistance;

        [SerializeField]
        [Header("HDR")]
        bool m_HDR = false;

        [SerializeField]
        [Header("Resolution Scale")]
        [Range(0.25f, 2f)]
        [Delayed]
        float m_ResolutionScale = 1;

        Lazy<Shader> m_defaultShader = new Lazy<Shader>(() => Shader.Find("SarRP/ForwardDefault"));
        Material m_defaultMaterial;


        [SerializeField]
        [HideInInspector]
        List<RenderPassAsset> m_RenderPasses = new List<RenderPassAsset>();
        public List<RenderPassAsset> RenderPasses => m_RenderPasses;

        public float MaxShadowDistance
        {
            get => m_MaxShadowDistance;
            set => m_MaxShadowDistance = value;
        }
        public bool HDR
        {
            get => m_HDR;
            set => m_HDR = value;
        }

        public float ResolutionScale
        {
            get => m_ResolutionScale;
            set => m_ResolutionScale = value;
        }

        public override Material defaultMaterial
        {
            get
            {
                if(!m_defaultMaterial)
                {
                    m_defaultMaterial = new Material(defaultShader);
                    m_defaultMaterial.SetShaderPassEnabled("MotionVectors", false);
                }
                return m_defaultMaterial;
            }
        }
        public override Shader defaultShader => m_defaultShader.Value;
        protected override RenderPipeline CreatePipeline()
        {
            return new SardineRenderPipeline(this);
        }
    }
}
