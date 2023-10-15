using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class SimulationController : MonoBehaviour
{
    private const int TreeAdd = 0;
    private const int TreeRemove = 1;
    private const int FireAdd = 2;
    private const int FireRemove = 3;

    public Camera mainCamera;
    public ForestGenerator Forest;
    public Wind Wind;
    public LayerMask TerrainLayer;
   
    [Header("Simulation")]
    public static float FireSpreadSpeed = 0.6f;
    public static float BurnSpeed = 0.8f;
    
    [Header("UI")]
    [SerializeField] private Button GenerateButton;
    [SerializeField] private Button ClearButton;
    
    [SerializeField] private Button SimButton;
    [SerializeField] private GameObject PlaySimText;
    [SerializeField] private GameObject StopSimText;
    
    [SerializeField] private Button AddRndFireButton;
    [SerializeField] private TMP_Dropdown ModeDropdown;
    [SerializeField] private Slider WindForceSlider;
    [SerializeField] private Slider WindDirSlider;
    [SerializeField] private Button ExitButton;

    private bool isRunning = false;
    private int currentMode = 0;
    private void Awake()
    {
        SetListeners();

        void SetListeners()
        {
            GenerateButton.onClick.AddListener(Generate);
            ClearButton.onClick.AddListener(Clear);
            SimButton.onClick.AddListener(PlaySimulation);
            AddRndFireButton.onClick.AddListener(AddRandomFire);
            ModeDropdown.onValueChanged.AddListener(OnModeSelected);
            
            WindDirSlider.onValueChanged.AddListener(OnWindDirChanged);
            WindForceSlider.onValueChanged.AddListener(OnWindForceChanged);
            
            ExitButton.onClick.AddListener(Exit);
        }
    }

    void Start()
    {
        OnWindDirChanged(WindDirSlider.value);
        OnWindForceChanged(WindForceSlider.value);
    }
    
    void Update () 
    {
        if(!Input.GetMouseButton(0))
            return;
        
        if (EventSystem.current.IsPointerOverGameObject(-1))
        {
            return;
        }
        
        if ( !Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000, TerrainLayer) )
        {
            return;
        }

        var pos = hit.point;
        
        switch (currentMode)
        {
            case TreeAdd: Forest.AddTreeAt(pos);
                break;
            case TreeRemove: Forest.RemoveTreeAt(pos);
                break;
            case FireAdd: Forest.AddFireAt(pos);
                break;
            case FireRemove: Forest.ExtinguishAt(pos);
                break;
        }
    }

    private void Generate() => Forest.Generate();

    private void Clear() => Forest.Clear();

    private void PlaySimulation()
    {
        isRunning = !isRunning;
        Forest.PlaySimulation(isRunning);
        
        PlaySimText.SetActive(!isRunning);
        StopSimText.SetActive(isRunning);
    }
    
    private void AddRandomFire() => Forest.AddRandomFire();

    private void OnModeSelected(int value) => currentMode = value;
    private void OnWindDirChanged(float value) => Wind.WindDirChange(value);
    private void OnWindForceChanged(float value) =>  Wind.WindForceChange(value);
    
    private void Exit()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
