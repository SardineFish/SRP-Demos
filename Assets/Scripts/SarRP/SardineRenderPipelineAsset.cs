using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace SarRP
{
    [CreateAssetMenu(fileName ="SarRenderPipeline", menuName = "SarRP/SarRenderPipeline")]
    public class SardineRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField]
        [Header("Max Shadow Distance")]
        float m_MaxShadowDistance;

        public float MaxShadowDistance
        {
            get => m_MaxShadowDistance;
            set => m_MaxShadowDistance = value;
        }
        protected override RenderPipeline CreatePipeline()
        {
            return new SardineRenderPipeline(this);
        }
    }
}
