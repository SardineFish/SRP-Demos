using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP
{
    public class HistoricalRTSystem
    {
        Dictionary<int, DoubleBuffer<RenderTexture>> rts = new Dictionary<int, DoubleBuffer<RenderTexture>>();
        
        public RenderTexture GetPrevious(int usage, Func<RenderTexture> allocator)
        {
            if (!rts.ContainsKey(usage))
                rts[usage] = new DoubleBuffer<RenderTexture>();
            if(!rts[usage].Current)
                rts[usage].Current = allocator();
            return rts[usage].Current;
        }
        public RenderTexture GetNext(int usage, Func<RenderTexture> allocator)
        {
            if (!rts.ContainsKey(usage))
                rts[usage] = new DoubleBuffer<RenderTexture>();
            if (!rts[usage].Next)
                rts[usage].Next = allocator();
            return rts[usage].Next;
        }
        public void Swap()
        {
            foreach(var item in rts)
            {
                item.Value.Flip();
            }
        }
    }
}
