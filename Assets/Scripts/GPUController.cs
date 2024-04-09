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

    [SerializeField, Range(2, 1000)]
	int nParticles = 10;

    [SerializeField, Range(1, 10)]
    float WPolyh = 0;
    [SerializeField, Range(1, 10)]
    float WSpikyh = 0;
    [SerializeField, Range(1, 10)]
    float WVisch = 0;

    [SerializeField, Range(0,15)]
    float viscosity;

    [SerializeField, Range(0,15)]
    float restDensity;

    [SerializeField, Range(5,40)]
    float stiffnessCoefficient = 1;

    [SerializeField, Range(0,15)]
    float tensionCoefficient = 1;

    [SerializeField, Range(1,30)]
    float gravityStrength = 2;

    [SerializeField, Range(0,3)]
    float timeStep;

    // [SerializeField, Range(1,100)]
    // int particleMass;

    [SerializeField, Range(1,50)]
    float boxSize = 5;

    [SerializeField, Range(1,10)]
    float boxCoeff = 0.001f;

    static readonly int
		positionsId = Shader.PropertyToID("particlePositions"),
		nParticlesID = Shader.PropertyToID("nParticles"),
        frameID = Shader.PropertyToID("frame"),
        gravityID = Shader.PropertyToID("gravityVector"),
        boxSizeID = Shader.PropertyToID("BOX_SCALE"),
        boxCoeffID = Shader.PropertyToID("BOX_INFLUENCE"),
        WPolyhID = Shader.PropertyToID("WPolyh"),
        WSpikyhID = Shader.PropertyToID("WSpikyh"),
        WVischID = Shader.PropertyToID("WVisch"),
        timeStepID = Shader.PropertyToID("timeStep"),
        // particleMassID = Shader.PropertyToID("particleMass"),
        viscosityID = Shader.PropertyToID("viscosityCoefficient"),
        restDensityID = Shader.PropertyToID("restDensity"),
        tensionCoefficientID = Shader.PropertyToID("tensionCoefficient"),
        stiffnessCoefficientID = Shader.PropertyToID("stiffnessCoefficient"),
        
		timeId = Shader.PropertyToID("time");

    int frame = 0;   
    int ParticleIntegrationKernel;
    int ParticleDensityCalculationKernel;

    void Integrate()
    {
        particleShader.SetBuffer(ParticleIntegrationKernel, positionsId, positionsBuffer);
        particleShader.SetBuffer(ParticleIntegrationKernel, "particleRead", frame % 2 == 0 ? particle0 : particle1);
        particleShader.SetBuffer(ParticleIntegrationKernel, "particleWrite", frame % 2 == 0 ? particle1 : particle0);
        int nGroups = Mathf.CeilToInt(nParticles / 64.0f);
		particleShader.Dispatch(ParticleIntegrationKernel, nGroups, 1, 1);

        frame++;
    } 

    void CalculateDensity()
    {
        particleShader.SetBuffer(ParticleDensityCalculationKernel, "particleRead", frame % 2 == 0 ? particle0 : particle1);
        particleShader.SetBuffer(ParticleDensityCalculationKernel, "particleWrite", frame % 2 == 0 ? particle1 : particle0);
        int nGroups = Mathf.CeilToInt(nParticles / 64.0f);
		particleShader.Dispatch(ParticleDensityCalculationKernel, nGroups, 1, 1);

        frame++;
    }

    void UpdateCoreShader()
    {
        float step = 10f / nParticles;
        particleShader.SetFloat(timeId, Time.time);
        particleShader.SetInt(nParticlesID, nParticles);
        particleShader.SetInt(frameID, frame);
        particleShader.SetVector(gravityID, Vector3.down * gravityStrength);
        particleShader.SetFloat(WPolyhID, WPolyh);
        particleShader.SetFloat(WSpikyhID, WSpikyh);
        particleShader.SetFloat(WVischID, WVisch);
        particleShader.SetFloat(timeStepID, timeStep);
        // particleShader.SetInt(particleMassID, particleMass);
        particleShader.SetFloat(viscosityID, viscosity);
        particleShader.SetFloat(restDensityID, restDensity);
        particleShader.SetFloat(tensionCoefficientID, tensionCoefficient);
        particleShader.SetFloat(stiffnessCoefficientID, stiffnessCoefficient);

        CalculateDensity();
        Integrate();

        particleShader.SetFloat(boxSizeID, boxSize);
        particleShader.SetFloat(boxCoeffID, boxCoeff);

        material.SetBuffer(positionsId, positionsBuffer);

        var bounds = new Bounds(Vector3.zero, Vector3.one * boxSize);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, positionsBuffer.count);
    }  

    void OnDrawGizmos()
    {
        var bounds = Vector3.one * (boxSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, bounds);
    }  

    void OnEnable() {
		positionsBuffer = new ComputeBuffer(nParticles, 3 * sizeof(float));
        particle0 = new ComputeBuffer(nParticles, 6 * sizeof(float) + 1 * sizeof(float));
        particle1 = new ComputeBuffer(nParticles, 6 * sizeof(float) + 1 * sizeof(float));

        ParticleIntegrationKernel = particleShader.FindKernel("ParticleLoop");
        ParticleDensityCalculationKernel = particleShader.FindKernel("ParticleDensity");
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
        UpdateCoreShader();
    }
}
