using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class LevelLink
{
    public string CurrentLevel;

    public string NextLevel;
}

[CreateAssetMenu(fileName = "LevelTransitions", menuName = "Scriptable Objects/LevelTransitions")]
public class LevelTransitions : ScriptableObject
{
    [SerializeField]
    private LevelLink[] Transitions = new LevelLink[]{};

    [SerializeField]
    private string Fallback;

    private Dictionary<string, string> TransitionNames;

    private void BuildMap()
    {
        TransitionNames = new Dictionary<string, string>();

        foreach(var trans in Transitions)
        {
            TransitionNames[trans.CurrentLevel] = trans.NextLevel;
        }
    }

    public string GetNextScene()
    {
        if(TransitionNames == null)
        {
            BuildMap();
        }
        
        string name = SceneManager.GetActiveScene().name;


        if(TransitionNames.TryGetValue(name, out string nextScene))
        {
            return nextScene;
        }
        else
        {
            return Fallback;
        }
    }
}
