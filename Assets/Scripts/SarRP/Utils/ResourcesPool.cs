using System;
using System.Collections.Generic;
using System.Linq;

namespace SarRP
{
    public abstract class ResourcesPool<T> where T : class
    {
        private static List<T> pool = new List<T>(64);
        protected static Func<T> initator { private get; set; }
        static ResourcesPool()
        {
            initator = _DefaultNew;
        }
        protected static T Get()
        {
            if (pool.Count <= 0)
                return initator();
            else
            {
                var res = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
                return res;
            }
        }
        protected static void Release(T res)
        {
            pool.Add(res);
        }
        private static T _DefaultNew()
        {
            return default;
        }
    }
}
