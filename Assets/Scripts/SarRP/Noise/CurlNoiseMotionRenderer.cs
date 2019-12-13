using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP.Noise
{
    public class CurlNoiseMotionRenderer
    {
        public Texture CurlNoise;
        public ComputeShader ComputeShdr;
        public Vector3Int Size;
        ComputeBuffer[] buffers;
        int currentUse = 0;
        ComputeBuffer CurrentBuffer => buffers[currentUse % 2];
        ComputeBuffer NextBuffer => buffers[(currentUse + 1) % 2];

        public CurlNoiseMotionRenderer(Texture curlNoise, ComputeShader computeShdr, Vector3Int size)
        {
            CurlNoise = curlNoise;
            ComputeShdr = computeShdr;
            Size = size;
            buffers = new ComputeBuffer[2]
            {
                new ComputeBuffer(Size.x*Size.y*Size.z, 3 * 4), // sizeof(float3)
                new ComputeBuffer(Size.x*Size.y*Size.z, 3 * 4),
            };
            var arr = new Vector3[Size.x * Size.y * Size.z];
            for (long i = 0; i < arr.LongLength; i++)
            {
                // i = (sizeX * SizeY) * z + SizeX * y + x
                var z = i / (Size.x * Size.y);
                var k = i % (Size.x * Size.y);
                var y = k / Size.x;
                var x = k % Size.x;
                var pos = new Vector3((float)x / Size.x, (float)y / Size.y, (float)z / Size.z);
                arr[i] = pos;
            }
            buffers[0].SetData(arr);
            buffers[1].SetData(arr);
        }

        public ComputeBuffer Update(CommandBuffer cmd)
        {
            currentUse ++;
            cmd.SetComputeBufferParam(ComputeShdr, 0, "CurrentBuffer", CurrentBuffer);
            cmd.SetComputeBufferParam(ComputeShdr, 0, "NextBuffer", NextBuffer);
            return NextBuffer;
        }
    }
}
