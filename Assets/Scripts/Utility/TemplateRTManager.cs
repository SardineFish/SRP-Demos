using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;


public class TemplateRTManager
{
    CommandBuffer cmd;
    Queue<int> availableRTs;
    List<int> RTs;
    public TemplateRTManager(CommandBuffer cmd, int initialCount, int width = -1, int height = -1, int depth = 0, FilterMode filterMode = FilterMode.Bilinear, RenderTextureFormat format = RenderTextureFormat.Default)
    {
        this.cmd = cmd;
        this.availableRTs = new Queue<int>(initialCount);
        this.RTs = new List<int>(initialCount);
        for (int i = 0; i < initialCount; i++)
        {
            var id = Shader.PropertyToID("__TEMPLATE_RT_" + i.ToString());
            cmd.GetTemporaryRT(id, width, height, depth, filterMode, format);
            availableRTs.Enqueue(id);
            this.RTs.Add(id);
        }
    }
    public int GetRT()
    {
        return availableRTs.Dequeue();
    }
    public void PutRT(int id)
    {
        availableRTs.Enqueue(id);
    }
    public void ReleaseRTs()
    {
        for (int i = 0; i < RTs.Count; i++)
            cmd.ReleaseTemporaryRT(RTs[i]);
    }
}