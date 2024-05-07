using UnityEngine;

public class GPUController : MonoBehaviour {

    ComputeBuffer positionsBuffer;
    ComputeBuffer particle0;
    ComputeBuffer particle1;

    ComputeBuffer particleCellsRead;
    ComputeBuffer particleCellsWrite;
    ComputeBuffer fixedParticleToCell;
    ComputeBuffer cellsStartIndices;
    ComputeBuffer initRadixCounters;
    ComputeBuffer radixToOffset;

    [SerializeField]
	Material material;

	[SerializeField]
	Mesh mesh;

    [SerializeField]
    ComputeShader particleShader;

    // [SerializeField, Range(2, 5000)]
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

    [SerializeField, Range(1,40)]
    float stiffnessCoefficient = 1;

    [SerializeField, Range(0,15)]
    float tensionCoefficient = 1;

    [SerializeField, Range(0,30)]
    float gravityStrength = 2;

    [SerializeField, Range(0,3)]
    float timeStep;

    // [SerializeField, Range(1,100)]
    // int particleMass;

    [SerializeField, Range(0.01f,5)]
    float boxNormA = 1;

    [SerializeField, Range(0,5)]
    float boxNormB = 0;

    [SerializeField, Range(1,100)]
    float boxSize = 5;

    [SerializeField, Range(0,20)]
    float boxCoeff = 1f;

    int cellsResolution = 1000;
    int cellsRadius = 50;
    int radixTuple = 8;
    int nParticlesPerThread;
    int nCountersPerThread;
    int nSummationThreadGroups;

    static readonly int
        cellsResolutionID = Shader.PropertyToID("cellsResolution"),
        cellsRadiusId = Shader.PropertyToID("cellsRadius"),
        particlesCellsReadID = Shader.PropertyToID("particlesCellsRead"),
        particlesCellsWriteID = Shader.PropertyToID("particlesCellsWrite"),
        fixedParticleToCellID = Shader.PropertyToID("fixedParticleToCell"),
        cellsStartIndicesID = Shader.PropertyToID("cellsStartIndices"),
        radixToOffsetID = Shader.PropertyToID("radixToOffset"),
        initRadixCountersID = Shader.PropertyToID("initRadixCounters"),
        nCountersPerThreadID = Shader.PropertyToID("nCountersPerThread"),
        passIdxID = Shader.PropertyToID("passIdx"),
        nParticlesPerThreadID = Shader.PropertyToID("nParticlesPerThread"),
        radixTupleID = Shader.PropertyToID("radixTuple "),

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
        particleMassID = Shader.PropertyToID("particleMass"),
        viscosityID = Shader.PropertyToID("viscosityCoefficient"),
        restDensityID = Shader.PropertyToID("restDensity"),
        normAID = Shader.PropertyToID("normA"),
        normBID = Shader.PropertyToID("normB"),
        tensionCoefficientID = Shader.PropertyToID("tensionCoefficient"),
        stiffnessCoefficientID = Shader.PropertyToID("stiffnessCoefficient"),
        timeId = Shader.PropertyToID("time");

    int frame = 0;   
    int ParticleIntegrationKernel;
    int ParticleDensityCalculationKernel;

    int CountRadixLocalKernel;
    int RadixOffsetPrefixSumKernel;
    int SortMapKernel;
    int AssignCellRegionsKernel;
    int ClearCountersKernel;

    void SortParticles()
    {
        int nPass = 32 / radixTuple;
        particleShader.SetBuffer(ClearCountersKernel, initRadixCountersID, initRadixCounters);
        particleShader.SetBuffer(CountRadixLocalKernel, initRadixCountersID, initRadixCounters);
        particleShader.SetBuffer(RadixOffsetPrefixSumKernel, initRadixCountersID, initRadixCounters);

        particleShader.SetBuffer(ClearCountersKernel, radixToOffsetID, radixToOffset);
        particleShader.SetBuffer(RadixOffsetPrefixSumKernel, radixToOffsetID, radixToOffset);
        particleShader.SetBuffer(SortMapKernel, radixToOffsetID, radixToOffset);
        
        int nCountingGroups = nParticles / (64 * nParticlesPerThread);
        int nCounters = (int)Mathf.Pow(2, radixTuple) * nCountingGroups;

        //larger than 4
        int nSummationGroups = nCounters / (64 * nCountersPerThread);

        for(int i = 0; i < nPass; i++)
        {
            particleShader.SetBuffer(CountRadixLocalKernel, particlesCellsReadID, i % 2 == 0 ?  particleCellsRead : particleCellsWrite);
            particleShader.SetBuffer(RadixOffsetPrefixSumKernel, particlesCellsReadID, i % 2 == 0 ?  particleCellsRead : particleCellsWrite);
            particleShader.SetBuffer(SortMapKernel, particlesCellsReadID, i % 2 == 0 ?  particleCellsRead : particleCellsWrite);

            particleShader.SetBuffer(CountRadixLocalKernel, particlesCellsWriteID, i % 2 == 0 ?  particleCellsWrite : particleCellsRead);
            particleShader.SetBuffer(RadixOffsetPrefixSumKernel, particlesCellsWriteID, i % 2 == 0 ?  particleCellsWrite : particleCellsRead);
            particleShader.SetBuffer(SortMapKernel, particlesCellsWriteID, i % 2 == 0 ?  particleCellsWrite : particleCellsRead);

            particleShader.SetInt(passIdxID, i);

            particleShader.Dispatch(CountRadixLocalKernel, nCountingGroups, 1, 1);
            particleShader.Dispatch(RadixOffsetPrefixSumKernel, nSummationGroups, 1, 1);
            particleShader.Dispatch(SortMapKernel, nCountingGroups, 1, 1);
            particleShader.Dispatch(ClearCountersKernel, nSummationGroups, 1, 1);
        }

        particleShader.SetBuffer(AssignCellRegionsKernel, cellsStartIndicesID, cellsStartIndices);
        particleShader.SetBuffer(AssignCellRegionsKernel, particlesCellsWriteID, particleCellsRead);    
        particleShader.Dispatch(AssignCellRegionsKernel, nCountingGroups, 1, 1);
    }

    void Integrate()
    {
        particleShader.SetBuffer(ParticleIntegrationKernel, fixedParticleToCellID, fixedParticleToCell);
        particleShader.SetBuffer(ParticleIntegrationKernel, cellsStartIndicesID, cellsStartIndices);

        particleShader.SetBuffer(ParticleIntegrationKernel, particlesCellsWriteID, particleCellsRead);
        particleShader.SetBuffer(ParticleIntegrationKernel, particlesCellsReadID, particleCellsWrite);

        particleShader.SetBuffer(ParticleIntegrationKernel, positionsId, positionsBuffer);
        particleShader.SetBuffer(ParticleIntegrationKernel, "particleRead", frame % 2 == 0 ? particle0 : particle1);
        particleShader.SetBuffer(ParticleIntegrationKernel, "particleWrite", frame % 2 == 0 ? particle1 : particle0);
        int nGroups = Mathf.CeilToInt(nParticles / 64.0f);
		particleShader.Dispatch(ParticleIntegrationKernel, nGroups, 1, 1);

        frame++;
    } 

    void CalculateDensity()
    {
        particleShader.SetBuffer(ParticleDensityCalculationKernel, fixedParticleToCellID, fixedParticleToCell);
        particleShader.SetBuffer(ParticleDensityCalculationKernel, cellsStartIndicesID, cellsStartIndices);

        particleShader.SetBuffer(ParticleDensityCalculationKernel, particlesCellsWriteID, particleCellsRead);
        particleShader.SetBuffer(ParticleDensityCalculationKernel, particlesCellsReadID, particleCellsWrite);

        particleShader.SetBuffer(ParticleDensityCalculationKernel, "particleRead", frame % 2 == 0 ? particle0 : particle1);
        particleShader.SetBuffer(ParticleDensityCalculationKernel, "particleWrite", frame % 2 == 0 ? particle1 : particle0);
        int nGroups = Mathf.CeilToInt(nParticles / 64.0f);
		particleShader.Dispatch(ParticleDensityCalculationKernel, nGroups, 1, 1);

        frame++;
    }

    void UpdateCoreShader()
    {
        particleShader.SetFloat(timeId, Time.time);
        particleShader.SetInt(nParticlesID, nParticles);
        particleShader.SetInt(frameID, frame);
        particleShader.SetVector(gravityID, Vector3.down * gravityStrength);
        particleShader.SetFloat(WPolyhID, WPolyh);
        particleShader.SetFloat(WSpikyhID, WSpikyh);
        particleShader.SetFloat(WVischID, WVisch);
        particleShader.SetFloat(timeStepID, timeStep);
        particleShader.SetInt(particleMassID, 1);
        particleShader.SetFloat(viscosityID, viscosity);
        particleShader.SetFloat(restDensityID, restDensity);
        particleShader.SetFloat(tensionCoefficientID, tensionCoefficient);
        particleShader.SetFloat(stiffnessCoefficientID, stiffnessCoefficient);
        particleShader.SetFloat(normAID, boxNormA);
        particleShader.SetFloat(normBID, boxNormB);
        particleShader.SetFloat(boxSizeID, boxSize);
        particleShader.SetFloat(boxCoeffID, boxCoeff);

        particleShader.SetInt(nParticlesPerThreadID, nParticlesPerThread);
        particleShader.SetInt(radixTupleID, radixTuple);
        particleShader.SetInt(nCountersPerThreadID, nCountersPerThread);

        CalculateDensity();
        Integrate();
        SortParticles();

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
        int floatSize = sizeof(float);
        int intSize = sizeof(int);

		positionsBuffer = new ComputeBuffer(nParticles, 3 * floatSize);
        particle0 = new ComputeBuffer(nParticles, 6 * floatSize + 1 * floatSize);
        particle1 = new ComputeBuffer(nParticles, 6 * floatSize + 1 * floatSize);
        particleCellsRead = new ComputeBuffer(nParticles, 2 * intSize);
        particleCellsWrite = new ComputeBuffer(nParticles, 2 * intSize);
        fixedParticleToCell = new ComputeBuffer(nParticles, 2 * intSize);
        cellsStartIndices = new ComputeBuffer(cellsResolution * cellsResolution, intSize);
        radixToOffset = new ComputeBuffer((int)Mathf.Pow(2, radixTuple), intSize);

        int nGroups = nParticles / (64 * nParticlesPerThread);
        initRadixCounters = new ComputeBuffer(256 * nGroups, intSize);

        int nCounters = (int)Mathf.Pow(2, radixTuple) * nGroups;
        int nSummationGroups = nCounters / (64 * nCountersPerThread);

        ParticleIntegrationKernel = particleShader.FindKernel("ParticleLoop");
        ParticleDensityCalculationKernel = particleShader.FindKernel("ParticleDensity");
        CountRadixLocalKernel = particleShader.FindKernel("CountRadixLocal");
        RadixOffsetPrefixSumKernel = particleShader.FindKernel("RadixOffsetPrefixSum");
        SortMapKernel = particleShader.FindKernel("SortMap");
        AssignCellRegionsKernel = particleShader.FindKernel("AssignCellRegions");
        ClearCountersKernel = particleShader.FindKernel("ClearCounters");
    }

    void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;

        particle0.Release();
        particle0 = null;

        particle1.Release();
        particle1 = null;

        particleCellsRead.Release();
        particleCellsRead = null;

        particleCellsWrite.Release();
        particleCellsWrite = null;

        fixedParticleToCell.Release();
        fixedParticleToCell = null;

        cellsStartIndices.Release();
        cellsStartIndices = null;

        radixToOffset.Release();
        radixToOffset = null;
    }

	void Update () 
    {
        UpdateCoreShader();
    }
}
