using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SarRP
{
    public static class IdentifierPool
    {
        static Queue<int> availableIDs = new Queue<int>();
        static Dictionary<int, int> usedIDs = new Dictionary<int, int>();
        static int nextId = 1;
        static IdentifierPool()
        {

        }
        public static int Get()
        {
            if (nextId >= 100)
                Debug.LogWarning("RenderTextures might be leaking.");
            if (availableIDs.Count <= 0)
                availableIDs.Enqueue(nextId++);
            var num = availableIDs.Dequeue();
            var id = Shader.PropertyToID($"RT_{num}");

            usedIDs[id] = num;
            return id;
        }
        public static void Release(int id)
        {
            if(usedIDs.ContainsKey(id))
            {
                var num = usedIDs[id];
                availableIDs.Enqueue(num);
            }
        }
    }
}
