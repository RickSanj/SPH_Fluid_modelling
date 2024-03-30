using UnityEngine;

public class GPUController : MonoBehaviour {

    ComputeBuffer positionsBuffer;
    ComputeBuffer particle0;
    ComputeBuffer particle1;

    [SerializeField]
	Material material;

	[SerializeField]
	Mesh mesh;

    [SerializeField]
    ComputeShader particleShader;

    [SerializeField, Range(10, 200)]
	int nParticles = 10;

    static readonly int
		positionsId = Shader.PropertyToID("particlePositions"),
		nParticlesID = Shader.PropertyToID("nParticles"),
		stepId = Shader.PropertyToID("step"),
        frameID = Shader.PropertyToID("frame"),
        gravityID = Shader.PropertyToID("gravityVector"),
		timeId = Shader.PropertyToID("time");

    int frame = 0;    

    void UpdateTestShader()
    {
        float step = 4f / nParticles;
        particleShader.SetFloat(stepId, step);
        particleShader.SetFloat(timeId, Time.time);
        particleShader.SetInt(nParticlesID, nParticles);
        particleShader.SetInt(frameID, frame);
        particleShader.SetVector(gravityID, Vector3.down);

        particleShader.SetBuffer(0, positionsId, positionsBuffer);
        particleShader.SetBuffer(0, "particleRead", frame % 2 == 0 ? particle0 : particle1);
        particleShader.SetBuffer(0, "particleWrite", frame % 2 == 0 ? particle1 : particle0);
        int nGroups = Mathf.CeilToInt(nParticles / 64f);
		particleShader.Dispatch(0, nGroups, 1, 1);

        material.SetFloat(stepId, step);
        material.SetBuffer(positionsId, positionsBuffer);

        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / nParticles));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, positionsBuffer.count);

        frame++;
    }    

    void OnEnable() {
		positionsBuffer = new ComputeBuffer(nParticles, 3 * 4);
        particle0 = new ComputeBuffer(nParticles, 5 * 8);
        particle1 = new ComputeBuffer(nParticles, 5 * 8);
	}

    void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;

        particle0.Release();
        particle0 = null;

        particle1.Release();
        particle1 = null;
    }

	void Update () 
    {
        UpdateTestShader();
    }
}
