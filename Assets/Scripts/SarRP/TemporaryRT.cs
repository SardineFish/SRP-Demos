using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP
{
    public struct TemporaryRT : IDisposable
    {
        CommandBuffer cmd;
        int id;
        public TemporaryRT(CommandBuffer cmd)
        {
            this.cmd = cmd;
            id = IdentifierPool.Get();
        }
        public void Dispose()
        {
            cmd.ReleaseTemporaryRT(id);
            IdentifierPool.Release(id);
        }
        public static implicit operator RenderTargetIdentifier(TemporaryRT rt) => rt.id;
        public static implicit operator int(TemporaryRT rt) => rt.id;
    }
}
