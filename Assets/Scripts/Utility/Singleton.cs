using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Singleton<T> : UnityEngine.MonoBehaviour where T:Singleton<T>
{
    public static T Instance;
    public Singleton() : base()
    {
        Instance = this as T;
    }
    protected virtual void Awake()
    {
        Instance = this as T;
    }
}