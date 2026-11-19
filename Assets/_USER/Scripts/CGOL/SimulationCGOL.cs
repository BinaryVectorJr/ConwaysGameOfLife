using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class SimulationCGOL : MonoBehaviour, ICallable
{
    // Create singleton
    public static SimulationCGOL cgolInstance;

    public Transform camTransform;

    // Tilemap info
    [SerializeField] 
    private Tilemap currentStateTilemap;
    [SerializeField] 
    private Tilemap nextStateTilemap;

    [SerializeField] 
    private Tile aliveTile;
    [SerializeField] 
    private Tile deadTile; // This is optional
    public int IPatternIndex { get; set; }
    [SerializeField]
    private TilePattern starterTilePattern;
    [SerializeField]
    private float simulationTimestep = 0.05f;   // To be used as seconds

    // Custom pattern
    public List<Vector2Int> patternGridCellsList;

    private bool editMode = false;

    // Tunable parameters
    [Header("Tunable")]
    [SerializeField]
    public int mooreMode = 0;
    [SerializeField]
    public List<int> originalRuleValues = new List<int> {2, 2, 3, 3, 3, 0};
    [SerializeField]
    public List<int> currentRuleValues = new List<int>();
    [SerializeField]
    private int currentIterations;
    [SerializeField]
    public int maxIterations;


    // Keeping track of alive cells via a Hashset - to not have to loop over large number of cells
    // Hashsets always store unique values
    private HashSet<Vector3Int> currentlyAliveSet = new HashSet<Vector3Int>();
    // private int deadAgentsCount = 0;
    private HashSet<Vector3Int> cellsToCheck = new HashSet<Vector3Int>();

    private Tile clickedOnTile = null;


    void Awake()
    {
        if(cgolInstance == null)
        {
            cgolInstance = this;
        }

        currentRuleValues = originalRuleValues;
    }

    void Start()
    {
        
    }

    public void ILocalLoadValues()
    {
        if(MainSimulation.mainInstance.currentActiveCanvas != null)
        {
            MainSimulation.mainInstance.currentSimulationType = SimulationType.Game_Of_Life;

            //currentRuleValues = originalRuleValues;
            currentRuleValues = MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().dropdownValues;
            MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().SetDropdownValues(currentRuleValues);
        }
    }

    public void ILocalSetup()
    {
        StopAllCoroutines();
        SetPatternInGrid(IPatternIndex);

        currentRuleValues = MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().dropdownValues;
        MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().SetDropdownValues(currentRuleValues);

        // Setting Moore Mode (3x3 grid for neighbor checking or 5x5 grid for neighbor checking)
        mooreMode = MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().dropdownValues[5];


        // TODO: This is a temporary limitation - make multithreaded later
        if(currentRuleValues.SequenceEqual(originalRuleValues))
        {
            foreach(var i in currentRuleValues)
            {
                print(i + " ");
            }
            maxIterations = 99999;
        }
        else
        {
            foreach(var i in currentRuleValues)
            {
                print(i + " ");
            }
            foreach(var j in originalRuleValues)
            {
                print(j + " ");
            }
            maxIterations = 500;
        }
    }

    public void ILocalStop()
    {
        StopAllCoroutines();
        currentRuleValues = MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().dropdownValues;
        MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().overlay.SetActive(false);
    }

    public void ILocalReset()
    {
        ClearTilesInGrid();
        currentIterations = 0;
        MainSimulation.mainInstance.labelTexts[1].text = "0";
        starterTilePattern = null;
        MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().SetDropdownValues(originalRuleValues);
        currentRuleValues = MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().dropdownValues;
        mooreMode = currentRuleValues[5];
        MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().ruleDropdowns[5].GetComponent<TMP_Dropdown>().interactable = true;
        MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().overlay.SetActive(false);

    }

    private void OnEnable()
    {
        StopAllCoroutines();
    }

    // public void LocalStart()
    // {   
    //     StopAllCoroutines();
    //     StartCoroutine(StartSimulation());
    // }

    void Update()
    {
        // MainSimulation.mainInstance.labelTexts[3].text = (1f/Time.deltaTime).ToString();
        // MainSimulation.mainInstance.labelTexts[2].text = currentlyAliveSet.Count.ToString();
        // MainSimulation.mainInstance.labelTexts[4].text = deadAgentsCount.ToString();

        HandleInputs();
    }


    // IMP: Main simulation loop - implemented as a coroutine rather than in Update since we want control of per timestep instead of per frame - DO NOT CHANGE BETWEEN DIFFERENT SIMULATION SCRIPTS
    private IEnumerator StartSimulation()
    {
        MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().overlay.SetActive(true);
        MainSimulation.mainInstance.currentActiveCanvas.GetComponent<SimulationRuleset>().ruleDropdowns[5].GetComponent<TMP_Dropdown>().interactable = false;

        // Create simple local cache for timestep instead of doing it on the heap
        // In this way memory is allocated once and its just reused every yield
        // DRAWBACK - Cannot change simulationTimestep mid-simulation, thus speed will always stay constant - can impact simulation speed control by user
        var stepInterval = new WaitForSeconds(simulationTimestep);

        // Trick to see our initial pattern atleast one time before it updates so we call yield once
        yield return stepInterval;

        // Do only when script is enabled - to play and pause
        while(enabled && currentIterations <= maxIterations)
        {
            UpdateTilemapState();

            // Here we can calculate metrics such as total population of alive cells, total time elapsed, how many steps simulation has taken etc.

            //yield return new WaitForSeconds(simulationTimestep);    // Allocates memory over heap and produces garbage every iteration
            yield return stepInterval;
        }
    }

    // Setting the initial alive tiles
    private void SetPatternInGrid(int _index)
    {
        ClearTilesInGrid();
        Vector2Int patternCenter;

        if(_index != 0)
        {
            starterTilePattern = MainSimulation.mainInstance.customTilePatterns[_index];

            // Get the center to create an offset so that the pattern aligns with the grid exactly
            patternCenter = starterTilePattern.GetCenterOfPatternForOffset();

            // Loop over all the cells in pattern grid
            for(int i = 0; i < starterTilePattern.patternGridCells.Length; i++)
            {
                // Converting to Vector3Int to facilitate usage in HashSet<Vector3>
                Vector3Int cellForTile = (Vector3Int)(starterTilePattern.patternGridCells[i] - patternCenter);
                currentStateTilemap.SetTile(cellForTile,aliveTile);

                // Add the alive cell to hash set to keep track
                currentlyAliveSet.Add(cellForTile);
            }
        }
        else
        {
            patternCenter = GetCenterOfPatternForOffset(patternGridCellsList);

            // Loop over all the cells in pattern grid
            for(int i = 0; i < patternGridCellsList.Count; i++)
            {
                // Converting to Vector3Int to facilitate usage in HashSet<Vector3>
                Vector3Int cellForTile = (Vector3Int)(patternGridCellsList[i] - patternCenter);
                currentStateTilemap.SetTile(cellForTile,aliveTile);

                // Add the alive cell to hash set to keep track
                currentlyAliveSet.Add(cellForTile);
            }
        }
    }

    private void ClearTilesInGrid()
    {
        currentStateTilemap.ClearAllTiles();
        nextStateTilemap.ClearAllTiles();
        currentlyAliveSet.Clear();
        cellsToCheck.Clear();

        // Reset any other values set in the StartSimulation coroutine here
    }

    private void UpdateTilemapState()
    {
        currentIterations++;
        MainSimulation.mainInstance.labelTexts[1].text = currentIterations.ToString();

        // Clear the previous state's cellsToCheck set
        cellsToCheck.Clear();

        if(mooreMode == 0)
        {
            // Get all the cells in that are currently alive and check their neighborhood and add those cells to the cellsToCheck hashset
            // We are checking the Moore Neigborhood - the 8 cells that surround the current cell (if its in the middle) in a 3x3 grid
            // If central is (0,0), one on left will be (-1,0), down is (0,-1) etc.
            foreach(Vector3Int cellLoc in currentlyAliveSet)
            {
                // Add the cells manually to skip over the for loop (unroll the two loops that would have been)
                cellsToCheck.Add(cellLoc);
                cellsToCheck.Add(cellLoc + new Vector3Int(-1,0,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(-1,1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(0,1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(1,1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(1,0,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(1,-1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(0,-1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(-1,-1,0));
            }
        }
        else    //5x5 Moore
        {
            foreach(Vector3Int cellLoc in currentlyAliveSet)
            {
                // Add the cells manually to skip over the for loop (unroll the two loops that would have been)
                cellsToCheck.Add(cellLoc);
                cellsToCheck.Add(cellLoc + new Vector3Int(-1,0,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(-1,1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(0,1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(1,1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(1,0,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(1,-1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(0,-1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(-1,-1,0));

                cellsToCheck.Add(cellLoc + new Vector3Int(-2,0,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(-2,1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(-2,2,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(-1,2,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(0,2,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(1,2,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(2,2,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(2,1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(2,0,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(2,-1,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(2,-2,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(1,-2,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(0,-2,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(-1,-2,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(-2,-2,0));
                cellsToCheck.Add(cellLoc + new Vector3Int(-2,-1,0));
            }
        }


        // CORE LOGIC FOR GAME OF LIFE
        // Loop over all the cells to check and apply the rules for Game Of Life
        // IMP: Game of Life Rules
        // It all boils down to check the number of alive neighbors of a cell. This gives us 4 simple rules, all of which are "outer totalistic" i.e. sum based rule instead of arrangement or pattern based rule excluding the central cell's state:
        // 1. UNDERPOPULATION (U) - Alive cells with fewer than 2 alive neighbors will die
        // 2. SURVIVAL (S) - Alive cells with 2 OR 3 alive neighbors will stay alive
        // 3. OVERPOPULATION (O) - Alive cells with more than 3 alive neighbors will die
        // 4. REPRODUCTION (R) - Dead cells with EXACTLY 3 alive neighbors comes back from the dead
        // We can represent this as a instruction tuple that is used in ABM as < world, states, neighborhood definition, update function>
        // So Game of Life can be written as <Z^2 (2D Grid), {0,1} (dead/alive), Moore Neighborhood (3x3 grid), outer totalistic (USOR)>
        foreach(Vector3Int cellLoc in cellsToCheck)
        {
            int currentCellAliveNeighbors = 0;
            // Count the current alive neighbors
            if(mooreMode == 0)
            {
                currentCellAliveNeighbors = CountConditionSatisfyingNeighbors(cellLoc, "Is Alive");
            }
            else
            {
                currentCellAliveNeighbors = CountConditionSatisfyingNeighbors2(cellLoc, "Is Alive");
            }

            // Check if cell is alive
            bool currentCellAlive = CheckIsConditionSatisfied(cellLoc, "Is Alive");

            // Check rules and update next state (i.e. nextStateTileset) so that we can update graphics
            // Then add to the alive set or remove from alive set to update the values (since its HashSet only unique values will be updated)
            if( !currentCellAlive && currentCellAliveNeighbors == currentRuleValues[4])
            {
                // REPRODUCTION - Back to life
                nextStateTilemap.SetTile(cellLoc, aliveTile);
                currentlyAliveSet.Add(cellLoc);
            }
            else if (currentCellAlive && (currentCellAliveNeighbors < currentRuleValues[0] || currentCellAliveNeighbors > currentRuleValues[3]))
            {
                // UNDERPOPULATED (less than 2) or OVERPOPULATED (more than 3) - Become dead
                nextStateTilemap.SetTile(cellLoc, deadTile);
                currentlyAliveSet.Remove(cellLoc);
            }
            else
            {
                // SURVIVAL - Stays alive (or dead) i.e. does not change state (get the value from current state tile map)
                nextStateTilemap.SetTile(cellLoc, currentStateTilemap.GetTile(cellLoc));
            }
        }

        // Swap the two tile sets so that the current one becomes next tile set and the next tile set becomes current tileset
        Tilemap previousStateTilemap = currentStateTilemap;
        currentStateTilemap = nextStateTilemap;
        nextStateTilemap = previousStateTilemap;

        // After swapping we don't need the values in nextStateTilemap so we can safely clear it
        nextStateTilemap.ClearAllTiles();

    }
    
    // Couning the neighbors that satisfy condition (Condition for CGoL = Alive)
    // Condition is a string that is purely to explain to programmer what the condition is that needs satisfying
    // Can make the parameter into attribute - no real performance overhead if kept this way though, just FYI
    private int CountConditionSatisfyingNeighbors(Vector3Int _centralCell, string _condition)
    {
        int aliveNeigborCount = 0;

        // Again could use two for loops here but better to unloop - performance gains are pretty okay once grid increases
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-1,0,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-1,1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(0,1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(1,1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(1,0,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(1,-1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(0,-1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-1,-1,0), "Is Alive")) {aliveNeigborCount += 1;}

        return aliveNeigborCount;
    }

    // For 5x5 Moore neighborhood
    private int CountConditionSatisfyingNeighbors2(Vector3Int _centralCell, string _condition)
    {
        int aliveNeigborCount = 0;

        // Again could use two for loops here but better to unloop - performance gains are pretty okay once grid increases
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-1,0,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-1,1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(0,1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(1,1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(1,0,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(1,-1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(0,-1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-1,-1,0), "Is Alive")) {aliveNeigborCount += 1;}

        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-2,0,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-2,1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-2,2,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-1,2,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(0,2,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(1,2,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(2,2,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(2,1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(2,0,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(2,-1,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(2,-2,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(1,-2,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(0,-2,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-1,-2,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-2,-2,0), "Is Alive")) {aliveNeigborCount += 1;}
        if(CheckIsConditionSatisfied(_centralCell + new Vector3Int(-2,-1,0), "Is Alive")) {aliveNeigborCount += 1;}

        return aliveNeigborCount;
    }

    // Condition function to check if a condition is satisfied (Condition to check for CGoL = Is neighbor cell alive)
    // Checking state of cell for alive or dead using the tile asset thats there in the location
    // Condition is a string that is purely to explain to programmer what the condition is that needs satisfying
    // Can make the parameter into attribute - no real performance overhead if kept this way though, just FYI
    private bool CheckIsConditionSatisfied(Vector3Int _cellToCheck, string _condition)
    {
        // Check if cell is Alive
        return currentStateTilemap.GetTile(_cellToCheck) == aliveTile;
    }

    // Mouse and Keyboard events
    private void HandleInputs()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            MainSimulation.mainInstance.cameraRigPosition.position = new Vector3(15.0f, 0.0f, -10.0f);
        }

        if(Input.GetKeyDown(KeyCode.CapsLock))
        {
            editMode = !editMode;
        }

        // Place tile where mouse is clicked on
        if(Input.GetMouseButtonDown(0) && editMode == true)
        {
            // Block events over UI
            if(EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            clickedOnTile = (Tile)currentStateTilemap.GetTile(MouseToTilemapCoords(Input.mousePosition));
            if(clickedOnTile == null)
            {
                currentStateTilemap.SetTile(MouseToTilemapCoords(Input.mousePosition), aliveTile);
                currentlyAliveSet.Add(MouseToTilemapCoords(Input.mousePosition));
            }
        }

        // Place tile when continued to be clicked on
        if(Input.GetMouseButton(0) && editMode == true)
        {
            // Block events over UI
            if(EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            clickedOnTile = (Tile)currentStateTilemap.GetTile(MouseToTilemapCoords(Input.mousePosition));
            if(clickedOnTile == null)
            {
                currentStateTilemap.SetTile(MouseToTilemapCoords(Input.mousePosition), aliveTile);
                currentlyAliveSet.Add(MouseToTilemapCoords(Input.mousePosition));
            }
        }

        // Remove tile where mouse is clicked on
        if(Input.GetMouseButtonDown(2) && editMode == true)
        {
            // Block events over UI
            if(EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            clickedOnTile = (Tile)currentStateTilemap.GetTile(MouseToTilemapCoords(Input.mousePosition));
            if(clickedOnTile == aliveTile)
            {
                currentStateTilemap.SetTile(MouseToTilemapCoords(Input.mousePosition), deadTile);
                currentlyAliveSet.Remove(MouseToTilemapCoords(Input.mousePosition));
            }
        }

        // Remove tile when continued to be clicked on
        if(Input.GetMouseButton(2) && editMode == true)
        {
            clickedOnTile = (Tile)currentStateTilemap.GetTile(MouseToTilemapCoords(Input.mousePosition));
            if(clickedOnTile == aliveTile)
            {
                currentStateTilemap.SetTile(MouseToTilemapCoords(Input.mousePosition), deadTile);
                currentlyAliveSet.Remove(MouseToTilemapCoords(Input.mousePosition));
            }
        }
    }

    // Mouse and Keyboard events
    // Get location from where mouse pointer clicked on
    private Vector3Int MouseToTilemapCoords(Vector2 _mouseClickPos)
    {
        // Get world position from mouse position in screen
        Vector3 worldPosAtMousePos = camTransform.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);

        // Convert Vector3 to Vector3Int to get tilemap position
        Vector3Int cellCoordsAtMousePos = currentStateTilemap.WorldToCell(worldPosAtMousePos);

        // For some reason the Z value gets set to -20 (probably due to version update from 2022.3.62 to 6. Anyway, this works)
        cellCoordsAtMousePos = new Vector3Int(cellCoordsAtMousePos.x, cellCoordsAtMousePos.y, 0);

        Debug.Log(cellCoordsAtMousePos);
        return cellCoordsAtMousePos;
    }

    // Calculate center of custom pattern
    public Vector2Int GetCenterOfPatternForOffset(List<Vector2Int> _patternGridCellsList)
    {
        Vector2Int[] _patternGridCells = _patternGridCellsList.ToArray();

        if(_patternGridCells == null || _patternGridCells.Length == 0)
        {
            return Vector2Int.zero;
        }

        // Get the bounds of all cells
        Vector2Int minBounds = Vector2Int.zero;
        Vector2Int maxBounds = Vector2Int.zero;

        // Loop over all cells to find the extent 
        for(int i = 0; i < _patternGridCells.Length; i++)
        {
            Vector2Int currentCell = _patternGridCells[i];

            // Compare and return the smaller of two values and set that as the new min
            minBounds.x = Mathf.Min(currentCell.x, minBounds.x);
            minBounds.y = Mathf.Min(currentCell.y, minBounds.y);

            // Compare and return the larger of two values and set that as the new max
            maxBounds.x = Mathf.Max(currentCell.x, maxBounds.x);
            maxBounds.y = Mathf.Max(currentCell.y, maxBounds.y);
        }

        return (minBounds + maxBounds)/2;
    }


}
