using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace SimpleRP
{
    [CreateAssetMenu(fileName ="SimpleRenderPipeline", menuName = "SimpleRP/SimpleRenderPipeline")]
    public class SimpleRenderPipelineAsset : RenderPipelineAsset
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
            return new SimpleRenderPipeline(this);
        }
    }
}
