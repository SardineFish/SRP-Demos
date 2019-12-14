using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Postprocess
{
    [CreateAssetMenu(fileName ="Fog", menuName ="SarRP/Postprocess/Fog")]
    public class Fog : PostprocessAsset
    {
        public float Near;
        public float Far;
        public float Density;
        [Range(0, 1)]
        public float Scale;
        public Color Color;
        public override PostprocessRenderer CreateRenderer()
        {
            return new FogRenderer(this);
        }
    }

    public class FogRenderer : PostprocessRenderer<Fog>
    {
        Material mat;
        public FogRenderer(Fog asset) : base(asset)
        {
            mat = new Material(Shader.Find("SarRP/Postprocess/Fog"));
        }
        public override void Render(PostprocessContext context)
        {
            if(!mat)
                mat = new Material(Shader.Find("SarRP/Postprocess/Fog"));
            var cmd = CommandBufferPool.Get("Fog");
            cmd.SetGlobalVector("_FogDistance", new Vector3(asset.Near, asset.Far, asset.Far - asset.Near));
            cmd.SetGlobalFloat("_Density", asset.Density);
            cmd.SetGlobalFloat("_Scale", asset.Scale);
            cmd.SetGlobalColor("_Color", asset.Color);
            cmd.BlitFullScreen(context.source, context.destination, mat, 0);
            context.ExecuteCommand(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
}
