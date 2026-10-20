using UnityEngine;

public class Puzzle : MonoBehaviour
{
    public int CorrectCount = 0;
    public int IncorrectCount = 0;

    public int CorrectToPass = 5;
    public int IncorrectToFail = 3;

    protected float TimeBetweenTests = 0.2f;
}