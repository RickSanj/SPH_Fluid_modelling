using UnityEngine;

public class GPUController : MonoBehaviour {

    ComputeBuffer positionsBuffer;

    [SerializeField]
	Material material;

	[SerializeField]
	Mesh mesh;

    [SerializeField]
    ComputeShader particleShader;

    static readonly int
		positionsId = Shader.PropertyToID("particlePositions"),
		nXId = Shader.PropertyToID("nParticlesX"),
        nYId = Shader.PropertyToID("nParticlesY"),
		stepId = Shader.PropertyToID("step"),
		timeId = Shader.PropertyToID("time");

    void UpdateTestShader()
    {
        float step = 4f / nParticlesX;
        particleShader.SetFloat(stepId, step);
        particleShader.SetFloat(timeId, Time.time);
        particleShader.SetInt(nXId, nParticlesX);
        particleShader.SetInt(nYId, nParticlesY);

        particleShader.SetBuffer(0, positionsId, positionsBuffer);
        int groupsX = Mathf.CeilToInt(nParticlesX / 8f);
        int groupsY = Mathf.CeilToInt(nParticlesY / 8f);
		particleShader.Dispatch(0, groupsX, groupsY, 1);

        material.SetFloat(stepId, step);
        material.SetBuffer(positionsId, positionsBuffer);

        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / Mathf.Min(nParticlesX, nParticlesY)));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, positionsBuffer.count);
    }    

	[SerializeField, Range(10, 200)]
	int nParticlesX = 10;

    [SerializeField, Range(10, 200)]
	int nParticlesY = 10;

    void OnEnable() {
		positionsBuffer = new ComputeBuffer(nParticlesY*nParticlesY, 3 * 4);
	}

    void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }

	void Update () 
    {
        UpdateTestShader();
    }
}
