using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarRP
{
    public class Singleton<T> where T : Singleton<T>, new()
    {
        static Lazy<T> instance = new Lazy<T>(() => new T());
        public static T Instance => instance.Value;
    }
}