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
        float step = 2f/nParticlesX;
        particleShader.SetFloat(stepId, step);
        particleShader.SetFloat(timeId, timeId.time);
        particleShader.SetInt(nXId, nParticlesX);
        particleShader.SetInt(nYId, nParticlesY);

        computeShader.SetBuffer(0, positionsId, positionsBuffer);
        int groupsX = Mathf.CeilToInt(nParticlesX / 8f);
        int groupsY = Mathf.CeilToInt(nParticlesY / 8f);
		computeShader.Dispatch(0, groupsX, groupsY, 1);

        Graphics.DrawMeshInstancedProcedural(mesh, 0, material);
    }    

	[SerializeField, Range(10, 200)]
	int nParticlesX = 10;

    [SerializeField, Range(10, 200)]
	int nParticlesY = 10;

	[SerializeField]
	FunctionLibrary.FunctionName function;

	public enum TransitionMode { Cycle, Random }

	[SerializeField]
	TransitionMode transitionMode = TransitionMode.Cycle;

	[SerializeField, Min(0f)]
	float functionDuration = 1f, transitionDuration = 1f;

	float duration;

	bool transitioning;

	FunctionLibrary.FunctionName transitionFunction;

    void Awake () {
		positionsBuffer = new ComputeBuffer(nParticles, 2 * 4);
	}
    void OnEnable() {
		positionsBuffer = new ComputeBuffer(nParticles, 2 * 4);
	}

    void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }

	void Update () 
    {
        duration += Time.deltaTime;
        if(duration >= functionDuration)
        {
			duration -= functionDuration;
			transitioning = true;
			transitionFunction = function;
			PickNextFunction();
		}

        UpdateTestShader();
    }

	void PickNextFunction ()
    {
        function = transitionMode == TransitionMode.Cycle ?
			FunctionLibrary.GetNextFunctionName(function) :
			FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    }
}
