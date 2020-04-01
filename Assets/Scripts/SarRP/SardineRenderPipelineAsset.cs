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
        protected override RenderPipeline CreatePipeline()
        {
            return new SardineRenderPipeline(this);
        }
    }
}
