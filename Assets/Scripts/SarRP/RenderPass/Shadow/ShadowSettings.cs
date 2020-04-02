using UnityEngine;
using System.Collections;

public enum ShadowAlgorithms : int
{
    Standard = 0,
    PSM = 1,
    TSM = 2,
    LiSPSM = 3,
}
[RequireComponent(typeof(Light))]
public class ShadowSettings : MonoBehaviour
{
    public bool Shadow = true;
    public ShadowAlgorithms Algorithms = ShadowAlgorithms.Standard;
    [Delayed]
    public int Resolution = 1024;
    public new Light light;
    public float MaxShadowDistance = 50;
    public float Bias = 0.01f;
    [Range(0, 23)]
    public float DepthBias = 1;
    [Range(0, 10)]
    public float NormalBias = 1;
    public float NearDistance = 0.1f;
    public float FocusDistance = 20;
    public bool Debug = false;

    private void Awake()
    {
        light = GetComponent<Light>();
    }
    private void Reset()
    {
        light = GetComponent<Light>();
    }
    private void Update()
    {
        Resolution = Mathf.ClosestPowerOfTwo(Resolution);
    }
}
