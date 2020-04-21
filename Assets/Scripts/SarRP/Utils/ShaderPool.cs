using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SarRP
{
    public static class ShaderPool
    {
        static Dictionary<string, Material> pool = new Dictionary<string, Material>();
        public static Material Get(string name)
        {
            if (!pool.ContainsKey(name) || !pool[name])
                pool[name] = new Material(Shader.Find(name));
            return pool[name];
        }
    }
}
