using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SimulationRuleset : MonoBehaviour
{
    public GameObject overlay;

    public List<GameObject> ruleDropdowns = new List<GameObject>();

    // Starter value for simulation - create 1 var for each sim
    [SerializeField]
    public List<int> dropdownValues = new List<int>();

    void Update()
    {
        //SimulationCGOL.cgolInstance.currentRuleValues = dropdownValues;
    }

    public void OnDropdownChange0(int _value)
    {
        dropdownValues[0] = _value;
    }

    public void OnDropdownChange1(int _value)
    {
        dropdownValues[1] = _value;
    }

    public void OnDropdownChange2(int _value)
    {
        dropdownValues[2] = _value;
    }

    public void OnDropdownChange3(int _value)
    {
        dropdownValues[3] = _value;
    }
        
    public void OnDropdownChange4(int _value)
    {
        dropdownValues[4] = _value;
    }

    public void OnDropdownChange5(int _value)
    {
        dropdownValues[5] = _value;
    }

    public void SetDropdownValues(List<int> _vals)
    {
        ruleDropdowns[0].GetComponent<TMP_Dropdown>().value = _vals[0];
        ruleDropdowns[0].GetComponent<TMP_Dropdown>().RefreshShownValue();

        ruleDropdowns[1].GetComponent<TMP_Dropdown>().value = _vals[1];
        ruleDropdowns[1].GetComponent<TMP_Dropdown>().RefreshShownValue();

        ruleDropdowns[2].GetComponent<TMP_Dropdown>().value = _vals[2];
        ruleDropdowns[2].GetComponent<TMP_Dropdown>().RefreshShownValue();

        ruleDropdowns[3].GetComponent<TMP_Dropdown>().value = _vals[3];
        ruleDropdowns[3].GetComponent<TMP_Dropdown>().RefreshShownValue();

        ruleDropdowns[4].GetComponent<TMP_Dropdown>().value = _vals[4];
        ruleDropdowns[4].GetComponent<TMP_Dropdown>().RefreshShownValue();

        ruleDropdowns[5].GetComponent<TMP_Dropdown>().value = _vals[5];
        ruleDropdowns[5].GetComponent<TMP_Dropdown>().RefreshShownValue();
    }
}
