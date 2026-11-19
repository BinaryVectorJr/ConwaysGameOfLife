using UnityEngine;
using UnityEngine.UI;

public class ToggleOn : MonoBehaviour
{
    public int customPatternIndex;
    public Vector2Int customPatternLocation;
    Toggle t;

    void Start()
    {
        t = GetComponent<Toggle>();
        t.onValueChanged.AddListener(delegate
        {
           ToggleValueChanged(t); 
        });        
    }

    void ToggleValueChanged(Toggle _t)
    {
        Image cb = _t.GetComponentInChildren<Image>();
        
        if(_t.isOn)
        {
            Debug.Log("Component " + _t.transform.name + "(index: " + (customPatternIndex-1) + ") is " + _t.isOn);
            SimulationCGOL.cgolInstance.patternGridCellsList.Add(customPatternLocation);
            cb.color = Color.blue;
        }
        else
        {
            SimulationCGOL.cgolInstance.patternGridCellsList.Remove(customPatternLocation);
            cb.color = Color.white;
        }
    }
}
