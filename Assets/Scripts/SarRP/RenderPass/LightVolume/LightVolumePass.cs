using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Renderer
{
    [CreateAssetMenu(fileName ="LightVolume", menuName ="SarRP/RenderPass/LightVolume")]
    public class LightVolumePass : RenderPassAsset
    {
        public override RenderPass CreateRenderPass()
        {
            return new LightVolumeRenderer(this);
        }
    }
    public class LightVolumeRenderer : RenderPassRenderer<LightVolumePass>
    {
        public LightVolumeRenderer(LightVolumePass asset) : base(asset)
        {

        }

        public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            for(var i = 0; i < renderingData.cullResults.visibleLights.Length; i++)
            {
                var light = renderingData.cullResults.visibleLights[i];
                if(light.light.GetComponent<Component.LightVolume>())
                {
                    RenderLightVolume(context, i, ref renderingData);
                }
            }
        }

        void RenderLightVolume(ScriptableRenderContext contect, int lightIndex, ref RenderingData renderingData)
        {
            var light = renderingData.cullResults.visibleLights[lightIndex];
            var volume = light.light.GetComponent<Component.LightVolume>();


        }
    }
}
