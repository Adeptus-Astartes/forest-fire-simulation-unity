using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class Wind : MonoBehaviour
{
    public static event Action<float[,]> OnWindChanged;
    
    [SerializeField] private GameObject WindArrow;
    [SerializeField] private float WindForceMultiplier = 4;
    private float[,] _windMatrix; //3x3 matrix
    private static float[,] _windBaseMatrix; //how fire spreads with no wind
    private Vector2[,] _windMatrixVectors;

    private float _windSpeed = 0;
    private float _windDir = 0;
    
    // Start is called before the first frame update
    void Awake()
    {
        //Active wind matrix
        _windMatrix = new float[3,3];
        //Base wind matrix. Used for multiplication. Decreased value for diagonal directions
        _windBaseMatrix = new float[3,3];
        //Vector wind matrix. Filled with normalized 2d vector that are pointed in all sides.
        _windMatrixVectors = new Vector2[3,3];
        
        
        for (int x = 0; x < 3; x++)
        for (int y = 0; y < 3; y++)
        {
            _windMatrixVectors[x,y] = new Vector2(x-1,y-1).normalized;
            
            if (x == 1 || y == 1)
                _windBaseMatrix[x, y] = 1f;
            else
                _windBaseMatrix[x, y] = 0.5f;
        }
    }

    public void WindForceChange(float value)
    {
        _windSpeed = value;
        UpdateWind();
    }
	
    public void WindDirChange(float value)
    {
        _windDir = value;
        UpdateWind();
        WindArrow.transform.localRotation = Quaternion.Euler(0,360 - value * (180 / Mathf.PI),0);
        
    }

    public void UpdateWind()
    {
        var windDirection = new Vector2(Mathf.Cos(_windDir), Mathf.Sin(_windDir));

        for (int i = 0; i < 3; i++)
        for (int j = 0; j < 3; j++)
        {
            var direction = WindForceMultiplier * Vector2.Dot( _windMatrixVectors[i, j],windDirection);
			
            _windMatrix[i, j] = _windBaseMatrix[i,j] + direction * _windSpeed;				

            if (_windMatrix[i, j] < 0)
            {
                _windMatrix[i, j] = 0;
            }
        }

        OnWindChanged?.Invoke(_windMatrix);
    }
    
}
