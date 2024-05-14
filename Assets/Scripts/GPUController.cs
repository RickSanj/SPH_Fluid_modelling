using UnityEngine;

public class GPUController : MonoBehaviour 
{
    ComputeBuffer positionsBuffer;
    ComputeBuffer particle0;
    ComputeBuffer particle1;

    ComputeBuffer particleCellsRead;
    ComputeBuffer particleCellsWrite;
    ComputeBuffer cellsStartIndices;
    ComputeBuffer cellCounters;
    ComputeBuffer cellToOffset;

    [SerializeField]
	Material material;

	[SerializeField]
	Mesh mesh;

    [SerializeField]
    ComputeShader particleShader;

    // [SerializeField, Range(2, 5000)]
	int nParticles = 3200;

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

    // [SerializeField, Range(0.01f,5)]
    // float boxNormA = 1;

    // [SerializeField, Range(0,5)]
    // float boxNormB = 0;

    [SerializeField, Range(1,150)]
    float boxSize = 5;

    [SerializeField, Range(0,20)]
    float boxCoeff = 1f;

    int cellsResolution = 64;
    int cellsRadius = 10;
    int nParticlesPerThread = 1;
    int nCellsPerThread = 2;

    static readonly int
        cellsResolutionID = Shader.PropertyToID("cellsResolution"),
        cellsRadiusId = Shader.PropertyToID("cellsRadius"),
        particlesCellsReadID = Shader.PropertyToID("particlesCellsRead"),
        particlesCellsWriteID = Shader.PropertyToID("particlesCellsWrite"),
        cellsStartIndicesID = Shader.PropertyToID("cellsStartIndices"),
        cellToOffsetID = Shader.PropertyToID("cellToOffset"),
        cellCountersID = Shader.PropertyToID("cellCounters"),
        nCellsPerThreadID = Shader.PropertyToID("nCellsPerThread"),
        nParticlesPerThreadID = Shader.PropertyToID("nParticlesPerThread"),
		positionsId = Shader.PropertyToID("particlePositions"),
		nParticlesID = Shader.PropertyToID("nParticles"),
        frameID = Shader.PropertyToID("frame"),
        gravityID = Shader.PropertyToID("gravityVector"),
        boxSizeID = Shader.PropertyToID("boxSize"),
        boxCoeffID = Shader.PropertyToID("boxInfluence"),
        WPolyhID = Shader.PropertyToID("WPolyh"),
        WSpikyhID = Shader.PropertyToID("WSpikyh"),
        WVischID = Shader.PropertyToID("WVisch"),
        timeStepID = Shader.PropertyToID("timeStep"),
        particleMassID = Shader.PropertyToID("particleMass"),
        viscosityID = Shader.PropertyToID("viscosityCoefficient"),
        restDensityID = Shader.PropertyToID("restDensity"),
        // normAID = Shader.PropertyToID("normA"),
        // normBID = Shader.PropertyToID("normB"),
        tensionCoefficientID = Shader.PropertyToID("tensionCoefficient"),
        stiffnessCoefficientID = Shader.PropertyToID("stiffnessCoefficient"),
        timeId = Shader.PropertyToID("time");

    int frame = 0;   
    int ParticleIntegrationKernel;
    int ParticleDensityCalculationKernel;
    int CountCellsKernel;
    int CellPrefixSumKernel;
    int SortMapKernel;
    int AssignCellRegionsKernel;
    int ClearCountersKernel;

    void ClearCounters()
    {
        particleShader.SetBuffer(ClearCountersKernel, cellCountersID, cellCounters);
        particleShader.SetBuffer(ClearCountersKernel, cellToOffsetID, cellToOffset);
        int nSummationGroups = (cellsResolution*cellsResolution) / (64 * nCellsPerThread);

        particleShader.Dispatch(ClearCountersKernel, nSummationGroups, 1, 1);
    }   

    void SortParticles()
    {
        particleShader.SetBuffer(CountCellsKernel, cellCountersID, cellCounters);
        particleShader.SetBuffer(CellPrefixSumKernel, cellCountersID, cellCounters);

        particleShader.SetBuffer(CellPrefixSumKernel, cellToOffsetID, cellToOffset);
        particleShader.SetBuffer(SortMapKernel, cellToOffsetID, cellToOffset);

        particleShader.SetBuffer(AssignCellRegionsKernel, cellsStartIndicesID, cellsStartIndices);
        
        int nCountingGroups = nParticles / (64 * nParticlesPerThread);
        int nSummationGroups = (cellsResolution*cellsResolution) / (64 * nCellsPerThread);

        particleShader.SetBuffer(CountCellsKernel, particlesCellsReadID, particleCellsRead);
        particleShader.SetBuffer(CellPrefixSumKernel, particlesCellsReadID, particleCellsRead);
        particleShader.SetBuffer(SortMapKernel, particlesCellsReadID, particleCellsRead);

        particleShader.SetBuffer(CountCellsKernel, particlesCellsWriteID, particleCellsWrite);
        particleShader.SetBuffer(CellPrefixSumKernel, particlesCellsWriteID, particleCellsWrite);
        particleShader.SetBuffer(SortMapKernel, particlesCellsWriteID, particleCellsWrite);

        particleShader.SetBuffer(AssignCellRegionsKernel, particlesCellsWriteID, particleCellsWrite);
        particleShader.SetBuffer(AssignCellRegionsKernel, particlesCellsReadID, particleCellsRead);

        particleShader.Dispatch(CountCellsKernel, nCountingGroups, 1, 1);

        // int[] counts = new int[cellsResolution*cellsResolution];
        // cellCounters.GetData(counts);
        // Debug.Log("-------------------------------------------------------------");
        // foreach(int count in counts)
        // {
        //     Debug.Log(count);
        // }

        particleShader.Dispatch(CellPrefixSumKernel, nSummationGroups, 1, 1);

        // int[] offsets = new int[cellsResolution*cellsResolution];
        // cellToOffset.GetData(offsets);

        // Debug.Log("-------------------------------------------------------------");
        // for(int i = 0; i < cellsResolution*cellsResolution; i++)
        // {
        //     if(offsets[i] == 0){continue;}
        //     Debug.Log("Cell " + i + " has offset " + offsets[i]);
        // }

        particleShader.Dispatch(SortMapKernel, nCountingGroups, 1, 1);

        // int[,] sorted = new int[nParticles, 2];
        // particleCellsWrite.GetData(sorted);

        // Debug.Log("-------------------------------------------------------------");
        // for(int i = 0; i < nParticles; i++)
        // {
        //     Debug.Log("At " + i + " pair:"+sorted[i,0] + "/" + sorted[i,1]);
        // }

        particleShader.Dispatch(AssignCellRegionsKernel, nCountingGroups, 1, 1);

        // int[] indices = new int[cellsResolution*cellsResolution];
        // cellsStartIndices.GetData(indices);

        // Debug.Log("-------------------------------------------------------------");
        // for(int i = 0; i < cellsResolution*cellsResolution; i++)
        // {
        //     if(indices[i] == 0){continue;}
        //     Debug.Log("Cell " + i + " has start at " + indices[i]);
        // }
    }

    void Integrate()
    {
        particleShader.SetBuffer(ParticleIntegrationKernel, cellsStartIndicesID, cellsStartIndices);
        particleShader.SetBuffer(ParticleIntegrationKernel, cellCountersID, cellCounters);

        particleShader.SetBuffer(ParticleIntegrationKernel, particlesCellsWriteID, particleCellsRead);
        particleShader.SetBuffer(ParticleIntegrationKernel, particlesCellsReadID, particleCellsWrite);

        particleShader.SetBuffer(ParticleIntegrationKernel, positionsId, positionsBuffer);
        particleShader.SetBuffer(ParticleIntegrationKernel, "particleRead", frame % 2 == 0 ? particle0 : particle1);
        particleShader.SetBuffer(ParticleIntegrationKernel, "particleWrite", frame % 2 == 0 ? particle1 : particle0);

        int nGroups = Mathf.CeilToInt(nParticles / 64.0f);
		particleShader.Dispatch(ParticleIntegrationKernel, nGroups, 1, 1);

        frame++;

        // Debug.Log("-------------------------------------------------------------");

        // int[,] newPairs = new int[nParticles,2];
        // particleCellsWrite.GetData(newPairs);

        // for(int i = 0; i < nParticles; i++)
        // {
        //     Debug.Log("Pair:"+newPairs[i,0]+";"+newPairs[i,1]);
        // }

        // if(frame > 4)
        // {
        //     Debug.Break();
        // }

        // Debug.Log("-------------------------------------------------------------");

        // int[,] newPairs = new int[nParticles,2];
        // particleCellsRead.GetData(newPairs);

        // for(int i = 0; i < nParticles; i++)
        // {
        //     Debug.Log("Pair:"+newPairs[i,0]+";"+newPairs[i,1]);
        // }
        
        // Debug.Log("+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");

        // int[,] newCoords = new int[nParticles, 2];
        // fixedParticleToCell.GetData(newCoords);

        // for(int i = 0; i < nParticles; i++)
        // {
        //     Debug.Log("Coords :"+newCoords[i,0]+";"+newCoords[i,1]);
        // }

        // Debug.Log("+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");

        // float[,] newAccelerations = new float[nParticles, 3];
        // accelerations.GetData(newAccelerations);

        // for(int i = 0; i < nParticles; i++)
        // {
        //     Debug.Log("Acceleration :"+newAccelerations[i,0]+";"+newAccelerations[i,1]+";"+newAccelerations[i,2]);
        // }

    } 

    void CalculateDensity()
    {
        particleShader.SetBuffer(ParticleDensityCalculationKernel, cellsStartIndicesID, cellsStartIndices);
        particleShader.SetBuffer(ParticleDensityCalculationKernel, cellCountersID, cellCounters);

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
        // particleShader.SetFloat(normAID, boxNormA);
        // particleShader.SetFloat(normBID, boxNormB);
        particleShader.SetFloat(boxSizeID, boxSize);
        particleShader.SetFloat(boxCoeffID, boxCoeff);

        particleShader.SetInt(cellsResolutionID, cellsResolution);
        particleShader.SetInt(cellsRadiusId, cellsRadius);
        particleShader.SetInt(nParticlesPerThreadID, nParticlesPerThread);
        particleShader.SetInt(nCellsPerThreadID, nCellsPerThread);

        Integrate();

        material.SetBuffer(positionsId, positionsBuffer);

        var bounds = new Bounds(Vector3.zero, Vector3.one * boxSize);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, positionsBuffer.count);

        ClearCounters();
        SortParticles();
        CalculateDensity();
    }  

    void OnDrawGizmos()
    {
        var bounds = Vector3.one * (boxSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, bounds);

        Gizmos.color = Color.blue;
        float cellSize = boxSize/(float)cellsResolution;

        for(int y = -cellsResolution/2; y < cellsResolution/2; y++)
        {
            Vector3 start = new Vector3(-boxSize/2, y*cellSize, 0);
            Vector3 end = new Vector3(boxSize/2, y*cellSize, 0);
            Gizmos.DrawLine(start, end);
        }

        for(int x = -cellsResolution/2; x < cellsResolution/2; x++)
        {
            Vector3 start = new Vector3(x*cellSize, -boxSize/2, 0);
            Vector3 end = new Vector3(x*cellSize , boxSize/2, 0);
            Gizmos.DrawLine(start, end);
        }
    }  

    void OnEnable() {
        int floatSize = sizeof(float);
        int intSize = sizeof(int);
        int nCells = cellsResolution * cellsResolution;

		positionsBuffer = new ComputeBuffer(nParticles, 3 * floatSize);
        particle0 = new ComputeBuffer(nParticles, 6 * floatSize + 1 * floatSize);
        particle1 = new ComputeBuffer(nParticles, 6 * floatSize + 1 * floatSize);

        particleCellsRead = new ComputeBuffer(nParticles, 2 * intSize);
        particleCellsWrite = new ComputeBuffer(nParticles, 2 * intSize);
        cellsStartIndices = new ComputeBuffer(nCells, intSize);
        cellToOffset = new ComputeBuffer(nCells, intSize);
        cellCounters = new ComputeBuffer(nCells, intSize);

        ParticleIntegrationKernel = particleShader.FindKernel("ParticleLoop");
        ParticleDensityCalculationKernel = particleShader.FindKernel("ParticleDensity");
        CountCellsKernel = particleShader.FindKernel("CountCells");
        CellPrefixSumKernel = particleShader.FindKernel("CellPrefixSum");
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

        cellsStartIndices.Release();
        cellsStartIndices = null;

        cellToOffset.Release();
        cellToOffset = null;
    }

	void Update () 
    {
        UpdateCoreShader();
    }
}
