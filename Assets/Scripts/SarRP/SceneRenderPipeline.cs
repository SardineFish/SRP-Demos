using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP
{
    [ExecuteInEditMode]
    public class SceneRenderPipeline : MonoBehaviour
    {
        public RenderPipelineAsset RenderPipelineAsset;
        private void Update()
        {
            if (GraphicsSettings.renderPipelineAsset != RenderPipelineAsset)
                GraphicsSettings.renderPipelineAsset = RenderPipelineAsset;
        }
    }
}
