using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface ICallable
{
    int IPatternIndex { get; set; }
    void ILocalSetup();
    void ILocalStop();
    void ILocalReset();
    void ILocalLoadValues();
}

// The values serve as IDs and positions in the lists as well - easier to do it this way
public enum SimulationType
{
    None = 0,
    Game_Of_Life = 1

    // Add more here if you want to
}

public enum Gamestate
{
    IDLE,
    RUNNING
}

public class MainSimulation : MonoBehaviour
{
    // Create singleton
    public static MainSimulation mainInstance { get; private set; }

    [Header("Simulation")]
    public Gamestate currentGamestate = Gamestate.IDLE;
    public SimulationType currentSimulationType = SimulationType.None;
    public string recordName = "P0";
    public Transform cameraRigPosition;

    [Header("Patterns")]
    [SerializeField] public List<TilePattern> customTilePatterns = new List<TilePattern>();
    [SerializeField] public Vector2Int[] startingTilePatternLocations;

    [Header("UI Objects and Prefabs")]
    [SerializeField] GameObject mainCanvas;
    [SerializeField] GameObject controlsCanvas;
    //[SerializeField] GameObject toggleObject;
    [SerializeField] GameObject patternDropdown;
    [SerializeField] GameObject customPatternCanvas;
    [SerializeField] List<GameObject> customCanvasSlots;
    [SerializeField] GameObject gridObject;
    [SerializeField] public GameObject currentActiveCanvas;
    [SerializeField] MonoBehaviour currentActiveSimulationScript;


    [Header("UI Lists")] 
    public List<GameObject> simulationSpecificCanvas;
    public List<TMP_Dropdown> dropdownsList;
    public List<Text> labelTexts = new List<Text>();    // UI life FPS, iterations etc
    public List<MonoBehaviour> simulationScripts = new List<MonoBehaviour>();

    void Awake()
    {
        if(mainInstance != null && mainInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        mainInstance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void SetSimulationType(SimulationType _sim)
    {
        mainInstance.currentSimulationType = _sim;
    }

    public void OnSimulationDropdownChanged(int _choice)
    {
        SimulationType sel = (SimulationType)_choice;
        mainInstance.SetSimulationType(sel);

        // Disable all scripts initially
        foreach(var s in simulationScripts)
        {
            s.enabled = false;
            mainInstance.currentActiveSimulationScript = null;
        }

        if(_choice-1 == -1)
        {
            foreach(var s in simulationScripts)
            {
                s.enabled = false;
                mainInstance.currentActiveSimulationScript = null;
                //toggleObject.GetComponent<Toggle>().interactable = false;
            }
        }
        else
        {
            mainInstance.simulationScripts[_choice-1].enabled = true;
            mainInstance.currentActiveSimulationScript =  mainInstance.simulationScripts[_choice-1];
            //toggleObject.GetComponent<Toggle>().interactable = true;

        }

        if(_choice == 0)
        {
            if(mainInstance.currentActiveCanvas != null)
            {
                Destroy(mainInstance.currentActiveCanvas);
                mainInstance.currentActiveCanvas = null;
                foreach(var item in labelTexts)
                {
                    item.gameObject.SetActive(false);
                }
            }

            // Hide Pattern dropdown
            patternDropdown.SetActive(false);
            customPatternCanvas.SetActive(false);

            // Unload all custom patterns
            OnCustomPatternToggle(false);
        }
        else
        {
            if(mainInstance.currentActiveCanvas != null)
            {
                Destroy(mainInstance.currentActiveCanvas);
                mainInstance.currentActiveCanvas = null;
            }
            mainInstance.currentActiveCanvas = Instantiate(simulationSpecificCanvas[_choice-1], mainCanvas.transform) as GameObject;
            
            if(mainInstance.currentActiveSimulationScript is ICallable callableScript1)
            {
                callableScript1.ILocalLoadValues();
            }
            foreach(var item in labelTexts)
            {
                item.gameObject.SetActive(true);
            }

            // Show Pattern dropdown
            patternDropdown.SetActive(true);
            customPatternCanvas.SetActive(true);

            // Load all custom patterns
            OnCustomPatternToggle(true);
        }
    }

    // To change starting pattern - Toggle
    public void OnCustomPatternToggle(bool _val)
    {
        mainInstance.dropdownsList[1].interactable = _val;
        if(_val == true)
        {
            LoadCustomPatterns();
        }
    }

    // To change starting pattern - Dropdown
    public void LoadCustomPatterns()
    {
        mainInstance.dropdownsList[1].ClearOptions();
        
        // Temporary
        mainInstance.dropdownsList[1].AddOptions(customTilePatterns.Select(p => p.name).ToList());

        // TODO: Later use this to convert the values to a pattern dynamically
    }

    public void OnPatternDropdownChanged(int _choice)
    {
        // Reset custom canvas for each slot
        if(_choice != 0)
        {
            foreach(var s in customCanvasSlots)
            {
                s.GetComponent<Toggle>().isOn = false;
            }
        }

        if(currentActiveSimulationScript && (mainInstance.dropdownsList[1].interactable == true))
        {
            if(mainInstance.currentActiveSimulationScript is ICallable callableScript1)
            {
                callableScript1.IPatternIndex = _choice;
                DisplayPattern(_choice);
            }
        }
    }

    public void DisplayPattern(int _choice)
    {
        Vector2Int[] onValues = customTilePatterns[_choice].patternGridCells;

        if(_choice == 0)
        {
            foreach(var s in customCanvasSlots)
            {
                s.GetComponent<Toggle>().isOn = false;
                s.GetComponent<Toggle>().interactable = true;
            }
        }
        else
        {
            foreach(var s in customCanvasSlots)
            {
                s.GetComponent<Toggle>().isOn = false;
                s.GetComponent<Toggle>().interactable = false;
            }

            foreach(var e in onValues)
            {
                if(customCanvasSlots.Any(x => x.transform.name == String.Concat(e.x," ",e.y)))
                {
                    customCanvasSlots.Find(x => x.transform.name == String.Concat(e.x," ",e.y)).GetComponent<Toggle>().isOn = true;
                }
            }
        }
    }

    void OnValidate()
    {
        simulationScripts = new List<MonoBehaviour>(gridObject.GetComponents<MonoBehaviour>());
    }

    public void OnSetupClick()
    {
        if(currentActiveSimulationScript)
        {
            if(mainInstance.currentActiveSimulationScript is ICallable callableScript)
            {
                callableScript.ILocalSetup();
            }
        }
    }

    public void OnStartClick()
    {
        if(currentActiveSimulationScript)
        {
            mainInstance.currentActiveSimulationScript.StartCoroutine("StartSimulation");
        }
    }

    public void OnStopClick()
    {
        if(currentActiveSimulationScript)
        {
            if(mainInstance.currentActiveSimulationScript is ICallable callableScript)
            {
                callableScript.ILocalStop();
            }
        }    
    }

    public void OnResetClick()
    {
        if(currentActiveSimulationScript)
        {
            if(mainInstance.currentActiveSimulationScript is ICallable callableScript)
            {
                callableScript.ILocalReset();
            }
        }
    }
}
