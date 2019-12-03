using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class WeightedItem : IWeightedObject
{
    [SerializeField]
    float weight = 1;
    public float Weight
    {
        get { return weight; }
        set { weight = value; }
    }

    public UnityEngine.Object Object;

    public WeightedItem(UnityEngine.Object @object, float weight = 1)
    {
        Weight = weight;
        Object = @object;
    }
}