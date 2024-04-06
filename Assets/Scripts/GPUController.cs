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

    [SerializeField, Range(2, 200)]
	int nParticles = 10;

    [SerializeField, Range(0, 10)]
    float WPolyh = 0;
    [SerializeField, Range(0, 10)]
    float WSpikyh = 0;
    [SerializeField, Range(0, 10)]
    float WVisch = 0;

    [SerializeField, Range(0,1)]
    float timeStep;

    [SerializeField, Range(1,50)]
    int particleMass;


    float boxSize = 5;
    float boxCoeff = 0.001f;

    static readonly int
		positionsId = Shader.PropertyToID("particlePositions"),
		nParticlesID = Shader.PropertyToID("nParticles"),
		stepId = Shader.PropertyToID("step"),
        frameID = Shader.PropertyToID("frame"),
        gravityID = Shader.PropertyToID("gravityVector"),
        boxSizeID = Shader.PropertyToID("BOX_SCALE"),
        boxCoeffID = Shader.PropertyToID("BOX_INFLUENCE"),
        WPolyhID = Shader.PropertyToID("WPolyh"),
        WSpikyhID = Shader.PropertyToID("WSpikyh"),
        WVischID = Shader.PropertyToID("WVisch"),
        timeStepID = Shader.PropertyToID("timeStep"),
        particleMassID = Shader.PropertyToID("particleMass"),
		timeId = Shader.PropertyToID("time");

    int frame = 0;    

    void UpdateTestShader()
    {
        float step = 10f / nParticles;
        particleShader.SetFloat(stepId, step);
        particleShader.SetFloat(timeId, Time.time);
        particleShader.SetInt(nParticlesID, nParticles);
        particleShader.SetInt(frameID, frame);
        particleShader.SetVector(gravityID, Vector3.down);
        particleShader.SetFloat(WPolyhID, WPolyh);
        particleShader.SetFloat(WSpikyhID, WSpikyh);
        particleShader.SetFloat(WVischID, WVisch);
        particleShader.SetFloat(timeStepID, timeStep);
        particleShader.SetInt(particleMassID, particleMass);

        particleShader.SetBuffer(0, positionsId, positionsBuffer);
        particleShader.SetBuffer(0, "particleRead", frame % 2 == 0 ? particle0 : particle1);
        particleShader.SetBuffer(0, "particleWrite", frame % 2 == 0 ? particle1 : particle0);
        int nGroups = Mathf.CeilToInt(nParticles / 64.0f);
		particleShader.Dispatch(0, nGroups, 1, 1);

        particleShader.SetFloat(boxSizeID, boxSize);
        particleShader.SetFloat(boxCoeffID, boxCoeff);

        material.SetFloat(stepId, step);
        material.SetBuffer(positionsId, positionsBuffer);

        var bounds = new Bounds(Vector3.zero, Vector3.one * (boxSize + boxSize / nParticles));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, positionsBuffer.count);

        frame++;
    }  

    void OnDrawGizmos()
    {
        var bounds = Vector3.one * (boxSize + boxSize / nParticles);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, bounds);
    }  

    void OnEnable() {
		positionsBuffer = new ComputeBuffer(nParticles, 3 * 8);
        particle0 = new ComputeBuffer(nParticles, 3 * 24 + 2 * 8);
        particle1 = new ComputeBuffer(nParticles, 3 * 24 + 2 * 8);

        // Debug.Break();
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
