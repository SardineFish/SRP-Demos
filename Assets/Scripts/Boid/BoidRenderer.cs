using SarRP;
using SarRP.Renderer;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;

struct EntityData
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 up;
    public Matrix4x4 rotation;
    public static int Size => sizeof(float) * 3 * 3 + sizeof(float) * 4 * 4;
}
[ImageEffectAllowedInSceneView]
public class BoidRenderer : UserPass
{
    public const int ComputeThreads = 1024;
    public const int KernelBoid = 0;
    public ComputeShader ComputeShader;

    public Material material;
    public Mesh mesh;

    [Delayed]
    public int Count = 1024;
    public float DistributeRadius = 10;
    public float MaxSpeed = 5;
    public float MinSpeed = 1;
    public Vector3 AngularLimit = new Vector3(.1f, .1f, .1f);
    public float AccelerationLimit = .5f;
    public Transform SpawnPoint;
    public bool ForceTarget = false;
    public Transform TargetPoint;

    public float SensoryRadius = 3;
    [Range(0,10)]
    public float Alignment = 1;
    [Range(0,10)]
    public float Seperation = 1;
    [Range(0,10)]
    public float Cohesion = 1;

    bool needUpdate = true;
    DoubleBuffer<ComputeBuffer> boidBuffer;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5];

    private void Awake()
    {
        SpawnPoint = transform;
    }

    [EditorButton]
    public void Reload()
    {
        FindObjectsOfType<BoidRenderer>().ForEach(renderer =>
        {
            renderer.needUpdate = true;
        });
        UnityEditor.SceneView.GetAllSceneCameras().Select(camera => camera.GetComponent<BoidRenderer>()).ForEach(renderer =>
        {
            renderer.needUpdate = true;
        });
    }

    void Update()
    {
        
    }

    public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if(needUpdate)
        {
            if (!ComputeShader || !material || !mesh)
                return;
            if(boidBuffer != null)
            {
                boidBuffer.Current.Release();
                boidBuffer.Next.Release();
            }
            if (argsBuffer != null)
                argsBuffer.Release();

            boidBuffer = new DoubleBuffer<ComputeBuffer>((i) => new ComputeBuffer((int)Count, EntityData.Size));
            var data = new EntityData[Count];
            for (var i = 0; i < Count; i++)
            {
                data[i] = new EntityData()
                {
                    position = Random.insideUnitSphere * DistributeRadius + SpawnPoint.position,
                    velocity = Random.insideUnitSphere,
                };
                data[i].velocity = data[i].velocity.normalized * (data[i].velocity.magnitude * (MaxSpeed - MinSpeed) + MinSpeed);
                var up = Random.onUnitSphere;
                var right = Vector3.Cross(data[i].velocity, up);
                if (Mathf.Approximately(right.magnitude, 0))
                    right = Vector3.right;
                up = Vector3.Cross(right, data[i].velocity);
                data[i].up = up.normalized;
            }
            boidBuffer.Current.SetData(data);
            boidBuffer.Next.SetData(data);
            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint)Count;
            args[2] = mesh.GetIndexStart(0);
            args[3] = mesh.GetBaseVertex(0);
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);
            
            needUpdate = false;
        }
    }

    public override void Render(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (boidBuffer == null)
            needUpdate = true;
        if (needUpdate)
            return;
        boidBuffer.Flip();
        var cmd = CommandBufferPool.Get("Boid");
        using (new ProfilingSample(cmd,"Boid"))
        {
            cmd.BeginSample("Boid Compute");
            cmd.SetComputeIntParam(ComputeShader, "TotalSize", Count);
            cmd.SetComputeFloatParam(ComputeShader, "SensoryRadius", SensoryRadius);
            cmd.SetComputeFloatParam(ComputeShader, "SeperationFactor", Seperation);
            cmd.SetComputeFloatParam(ComputeShader, "AlignmentFactor", Alignment);
            cmd.SetComputeFloatParam(ComputeShader, "CohesionFactor", Cohesion);
            cmd.SetComputeFloatParam(ComputeShader, "DeltaTime", Time.deltaTime);
            cmd.SetComputeVectorParam(ComputeShader, "SpeedLimit", new Vector2(MinSpeed, MaxSpeed));
            cmd.SetComputeVectorParam(ComputeShader, "AngularSpeedLimit", AngularLimit);
            cmd.SetComputeFloatParam(ComputeShader, "AccelerationLimit", AccelerationLimit);
            cmd.SetComputeVectorParam(ComputeShader, "Target", TargetPoint.transform.position.ToVector4(ForceTarget ? 1 : 0));
            cmd.SetComputeBufferParam(ComputeShader, KernelBoid, "InputBuffer", boidBuffer.Current);
            cmd.SetComputeBufferParam(ComputeShader, KernelBoid, "OutputBuffer", boidBuffer.Next);
            cmd.DispatchCompute(ComputeShader, KernelBoid, Mathf.CeilToInt(Count / ComputeThreads), 1, 1);
            cmd.EndSample("Boid Compute");

            cmd.BeginSample("Boid Rendering");
            var light = GetMainLight(renderingData);
            if (light.light)
            {
                cmd.SetGlobalVector("_MainLightPosition", light.light.transform.forward.ToVector4(0));
                cmd.SetGlobalColor("_MainLightColor", light.finalColor);
            }
            cmd.SetGlobalColor("_AmbientLight", RenderSettings.ambientLight);
            cmd.SetGlobalVector("_WorldCameraPos", renderingData.camera.transform.position);
            cmd.SetGlobalBuffer("boidBuffer", boidBuffer.Next);
            cmd.DrawMeshInstancedIndirect(mesh, 0, material, 0, argsBuffer);
            cmd.EndSample("Boid Rendering");
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);

    }

    VisibleLight GetMainLight(RenderingData renderingData)
    {
        var lights = renderingData.cullResults.visibleLights;
        var sun = RenderSettings.sun;
        if (sun == null)
            return default;
        for (var i = 0; i < lights.Length; i++)
        {
            if (lights[i].light == sun)
                return lights[i];
        }
        return default;
    }
}
