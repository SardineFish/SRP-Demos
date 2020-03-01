using UnityEngine;
using System.Collections;

public enum ShadowAlgorithms
{
    Simple,
    TSM,
    PSM,
    LiSPSM,
}
[RequireComponent(typeof(Light))]
public class ShadowSettings : MonoBehaviour
{
    public bool Shadow = true;
    public ShadowAlgorithms Algorithms = ShadowAlgorithms.Simple;
    [Delayed]
    public int Resolution = 1024;
    public Light light;
    public float MaxShadowDistance = 50;
    public float Bias = 0.01f;

    private void Awake()
    {
        this.light = GetComponent<Light>();
    }
    private void Update()
    {
        Resolution = Mathf.ClosestPowerOfTwo(Resolution);
    }
}
