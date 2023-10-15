using UnityEngine;
using Random = UnityEngine.Random;

public class ForestChunk : MonoBehaviour
{
	
	[SerializeField] private Mesh instanceMesh;
	[SerializeField] private Material instanceMaterial;
	[SerializeField] private int subMeshIndex = 0;

	public ForestChunk[,] OtherParts;
	
	private ForestGenerator _controller;
	private int _chunkSize;
	private Vector2Int _chunkPos;
	
	private bool isActive = false;
	
	private byte[][] _treeBitMap;
	private const byte Fire = 64;
	private const byte Burned = 255;
	
	private int _instanceCount;
	private int _cachedInstanceCount = -1;
	private int _cachedSubMeshIndex = -1;
	private ComputeBuffer _positionBuffer;
	private ComputeBuffer _colorBuffer;
	private ComputeBuffer _argsBuffer;
	private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };

	private const string POSITON_BUFFER_NAME = "positionBuffer";
	private const string COLOR_BUFFER_NAME = "colorBuffer";

	private Vector4[] positions;
	private Vector3[] colors;
	
	private float[,] _windMatrix;

	private void OnEnable()
	{
		Wind.OnWindChanged += OnWindUpdated;
	}

	private void OnDisable()
	{
		Wind.OnWindChanged -= OnWindUpdated;
	}

	public void Setup(ForestGenerator controller, int chunkSize, Vector2Int chunkPos)
	{
		_controller = controller;
		_chunkSize = chunkSize;
		_chunkPos = chunkPos;
		_instanceCount = chunkSize * chunkSize;
		
		instanceMaterial = Instantiate(instanceMaterial);
	}

	public void ActivateChunk()
	{
		_treeBitMap = new byte[_chunkSize][];
		for (int i = 0; i < _chunkSize; i++)
		{
			_treeBitMap[i] = new byte[_chunkSize];
		}
		
		positions = new Vector4[_instanceCount];
		colors = new Vector3[_instanceCount];
		
		AllocateBuffers();
		GenerateTrees();
		UpdatePositionsBuffer();
		UpdateColorsBuffer();
		
		isActive = true;
	}
	
	public void DisposeChunk()
	{
		for (var x = 0; x < _chunkSize; x++)
		for (var y = 0; y < _chunkSize; y++)
			_treeBitMap[x][y] = 0;

		_positionBuffer?.Release();
		_positionBuffer = null;

		_colorBuffer?.Release();
		_colorBuffer = null;

		_argsBuffer?.Release();
		_argsBuffer = null;

		positions = null;
		colors = null;
		
		isActive = false;
	}

	private void GenerateTrees()
	{
		int index = 0;
		for (var x = 0; x < _chunkSize; x++)
		for (var y = 0; y < _chunkSize; y++)
		{
			var tree = ForestSettings.TreeProbe(_chunkPos, x, y);
			_treeBitMap[x][y] = tree;
			if (tree == 0) continue;
			
			var pos = _controller.GetHeight(new Vector3( _chunkPos.x * _chunkSize + x, 0, _chunkPos.y * _chunkSize + y));
			positions[index] = new Vector4(pos.x, pos.y, pos.z, 1);
			index++;
		}
	}

	private void Update()
	{
		if(!isActive)
			return;
		
		Graphics.DrawMeshInstancedIndirect(
			instanceMesh, 
			subMeshIndex, 
			instanceMaterial, 
			new Bounds(transform.position, Vector3.one * _chunkSize * 2), 
			_argsBuffer);
	}

	public void UpdateChunk()
	{
		UpdateFire();
		UpdateColorsBuffer();
	}

	private void AllocateBuffers()
	{
		_positionBuffer?.Release();
		_colorBuffer?.Release();
		_argsBuffer?.Release();
		
		_positionBuffer = new ComputeBuffer(_instanceCount, 16);
		_colorBuffer = new ComputeBuffer(_instanceCount, 12);
		_argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		
		// Indirect args
		if (instanceMesh != null) {
			_args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
			_args[1] = (uint)_instanceCount;
			_args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
			_args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
		}
		else
		{
			_args[0] = _args[1] = _args[2] = _args[3] = 0;
		}
		
		_argsBuffer.SetData(_args);

		_cachedInstanceCount = _instanceCount;
		_cachedSubMeshIndex = subMeshIndex;
	}
	
	private void UpdatePositionsBuffer() 
	{
		if (instanceMesh != null)
			subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

		int index = 0;
		for (var x = 0; x < _chunkSize; x++)
		for (var y = 0; y < _chunkSize; y++)
		{
			var tree = _treeBitMap[x][y];
			if (tree == 0) continue;
			
			var pos = _controller.GetHeight(new Vector3( _chunkPos.x * _chunkSize + x, 0, _chunkPos.y * _chunkSize + y));

			positions[index] = new Vector4(pos.x,pos.y,pos.z, 1);
			index++;
		}

		_positionBuffer.SetData(positions);
		instanceMaterial.SetBuffer(POSITON_BUFFER_NAME, _positionBuffer);
	}
	
	private void UpdateColorsBuffer() 
	{
		int index = 0;
		for (var x = 0; x < _chunkSize; x++)
		for (var y = 0; y < _chunkSize; y++)
		{
			var tree = _treeBitMap[x][y];
			if (tree == 0) continue;

			var color = new Vector3(0, 0, 0);
			if (tree < Fire)
			{
				color = new Vector3(0, 0.8f, 0);
			}
			else if (tree < Burned)
			{
				color = new Vector3(1, 0, 0);
			}

			colors[index] = color;
			
			index++;
		}

		_colorBuffer.SetData(colors);
		instanceMaterial.SetBuffer(COLOR_BUFFER_NAME, _colorBuffer);
	}
	
	void UpdateFire()
	{
		for (var x = 0; x < _chunkSize; x++)
		for (var y = 0; y < _chunkSize; y++)
		{
			var tree = _treeBitMap[x][y];
			if (tree == 0) continue;
			
			if (tree < Fire && Random.value < SimulationController.FireSpreadSpeed)
			{
				float affectedTree = 0f;

				if (x > 0 && x < _chunkSize - 1 && y > 0 && y < _chunkSize - 1)
				{
					var t = _treeBitMap[x + 1][y];
					
					if (t >= Fire && t < Burned) affectedTree += _windMatrix[2, 1];
					t = _treeBitMap[x - 1][y];
					if (t >= Fire && t < Burned) affectedTree += _windMatrix[0, 1];
					t = _treeBitMap[x][y + 1];
					if (t >= Fire && t < Burned) affectedTree += _windMatrix[1, 2];
					t = _treeBitMap[x][y - 1];
					if (t >= Fire && t < Burned) affectedTree += _windMatrix[1, 0];

					t = _treeBitMap[x + 1][y + 1];
					if (t >= Fire && t < Burned) affectedTree += _windMatrix[2, 2];
					t = _treeBitMap[x - 1][y + 1];
					if (t >= Fire && t < Burned) affectedTree += _windMatrix[0, 2];
					t = _treeBitMap[x + 1][y - 1];
					if (t >= Fire && t < Burned) affectedTree += _windMatrix[2, 0];
					t = _treeBitMap[x - 1][y - 1];
					if (t >= Fire && t < Burned) affectedTree += _windMatrix[0, 0];
				}
				else 
				{
					affectedTree += _windMatrix[0, 1] * IsFire(x - 1, y)
					                + _windMatrix[1, 0] * IsFire(x, y - 1)
					                + _windMatrix[1, 2] * IsFire(x, y + 1)
					                + _windMatrix[2, 1] * IsFire(x + 1, y);

					affectedTree += _windMatrix[2, 2] * IsFire(x + 1, y + 1)
					                + _windMatrix[0, 2] * IsFire(x - 1, y + 1)
					                + _windMatrix[2, 0] * IsFire(x + 1, y - 1)
					                + _windMatrix[0, 0] * IsFire(x - 1, y - 1);
				}

				_treeBitMap[x][y] += (byte) affectedTree;
			}
			
			if (tree >= Fire && tree < Burned && Random.value < SimulationController.BurnSpeed)
			{
				_treeBitMap[x][y]++;
			}
		}
	}
	
	public void AddRandomFire()
	{
		for (var i = 0; i < 10; i++)
		{
			var x = Random.Range(0, _chunkSize);
			var y = Random.Range(0, _chunkSize);
			
			if (_treeBitMap[x][y] > 0 && _treeBitMap[x][y] < Fire)
			{
				_treeBitMap[x][y] = Fire;
				break;
			}
		}
	}

	private int IsFire(int x, int y)
	{
		if (x >= 0 && x < _chunkSize && y >= 0 && y < _chunkSize)
		{
			var tree = _treeBitMap[x][y];
			return tree >= Fire && tree < Burned ? 1 : 0;
		}
		var newX = _chunkPos.x;
		var newY = _chunkPos.y;
		if (x < 0)
		{
			newX--;
			x += _chunkSize;
		}
		else if (x >= _chunkSize)
		{
			newX++;
			x -= _chunkSize;
		}
		if (y < 0)
		{
			newY--;
			y += _chunkSize;
		}
		else if (y >= _chunkSize)
		{
			newY++;
			y -= _chunkSize;
		}

		if (newX < 0 || newX >= ForestGenerator.ForestSize.x || newY < 0 || newY >= ForestGenerator.ForestSize.y)
		{
			return 0;
		}
		
		return OtherParts[newX, newY].IsFire(x, y);
	}

	public void AddTreeAt(Vector3 worldCoords)
	{
		var x = Mathf.FloorToInt(worldCoords.x - _chunkSize * _chunkPos.x);
		var y = Mathf.FloorToInt(worldCoords.z - _chunkSize * _chunkPos.y);
		if (_treeBitMap[x][y] == 0)
		{
			_treeBitMap[x][y] = 1;
		}

		UpdatePositionsBuffer();
	}
	
	public void RemoveTreeAt(Vector3 worldCoords)
	{
		var x = Mathf.FloorToInt(worldCoords.x - _chunkSize * _chunkPos.x);
		var y = Mathf.FloorToInt(worldCoords.z - _chunkSize * _chunkPos.y);
		if (_treeBitMap[x][y] > 0)
		{
			_treeBitMap[x][y] = 0;
		}

		UpdatePositionsBuffer();
	}

	public void AddFireAt(Vector3 worldCoords)
	{
		var x = Mathf.FloorToInt(worldCoords.x - _chunkSize * _chunkPos.x);
		var y = Mathf.FloorToInt(worldCoords.z - _chunkSize * _chunkPos.y);
		if (_treeBitMap[x][y] > 0 && _treeBitMap[x][y] < Fire)
		{
			_treeBitMap[x][y] = Fire;
		}
	}


	public void ExtinguishAt(Vector3 worldCoords)
	{
		var x = Mathf.FloorToInt(worldCoords.x - _chunkSize * _chunkPos.x);
		var y = Mathf.FloorToInt(worldCoords.z - _chunkSize * _chunkPos.y);
		if (IsFire(x, y) == 1)
		{
			_treeBitMap[x][y] = 1;
		}
		
		if (x > 0 && x < _chunkSize - 1 && y > 0 && y < _chunkSize - 1)
		{
			if (IsFire(x - 1, y) == 1) _treeBitMap[x - 1][y] = 1;
			if (IsFire(x + 1, y) == 1) _treeBitMap[x + 1][y] = 1;
			if (IsFire(x, y + 1) == 1) _treeBitMap[x][y + 1] = 1;
			if (IsFire(x, y - 1) == 1) _treeBitMap[x][y - 1] = 1;
			if (IsFire(x + 1, y + 1) == 1) _treeBitMap[x + 1][y + 1] = 1;
			if (IsFire(x - 1, y - 1) == 1) _treeBitMap[x - 1][y - 1] = 1;
			if (IsFire(x + 1, y - 1) == 1) _treeBitMap[x + 1][y - 1] = 1;
			if (IsFire(x - 1, y + 1) == 1) _treeBitMap[x - 1][y + 1] = 1;
		}
	}
	
	private void OnWindUpdated(float[,] windMatrix) => _windMatrix = windMatrix;
}
