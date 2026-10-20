using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


[Serializable]
public struct Circle
{
    public Image Img;

    [field: SerializeField] public float Radius { get; private set; }

    public void SetRadius(float newRad)
    {
        Radius = newRad;
        Img.rectTransform.localScale = Vector3.one * Radius;
    }
}

public class ShapeSize : Puzzle
{

    public Circle Circle1, Circle2;

    public float MinRadius = 1.0f;
    public float MaxRadius = 3.0f;


    public static event Action<bool> OnTestsFinished;

    public static void ClearFinishedEvents()
    {
        OnTestsFinished = null;
    }

    void Start()
    {
        StartCoroutine(NextTest(true));
    }

    private IEnumerator NextTest(bool firstRun = false)
    {
        if(CorrectCount == CorrectToPass || IncorrectCount == IncorrectToFail)
        {
            OnTestsFinished?.Invoke(CorrectCount == CorrectToPass);
            SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("Puzzle_ShapeSize"));
        }
        else
        {
            Circle1.Img.gameObject.SetActive(false);
            Circle2.Img.gameObject.SetActive(false);
            
            if(!firstRun)
            {
                yield return new WaitForSecondsRealtime(TimeBetweenTests);
            }


            Circle1.Img.gameObject.SetActive(true);
            Circle2.Img.gameObject.SetActive(true);


            float rad1 = UnityEngine.Random.Range(MinRadius, MaxRadius);
            float rad2 = UnityEngine.Random.Range(MinRadius, MaxRadius);

            while(Mathf.Abs(rad1 - rad2) < 0.4f)
            {
                rad2 = UnityEngine.Random.Range(MinRadius, MaxRadius);
            }

            Circle1.SetRadius(rad1);
            Circle2.SetRadius(rad2);
        }
    }

    public void OnCircle1Clicked()
    {
        if(Circle1.Radius > Circle2.Radius)
        {
            CorrectCount++;
        }
        else
        {
            IncorrectCount++;
        }

        StartCoroutine(NextTest());
    }

    public void OnCircle2Clicked()
    {
        if(Circle2.Radius > Circle1.Radius)
        {
            CorrectCount++;
        }
        else
        {
            IncorrectCount++;
        }

        StartCoroutine(NextTest());
    }


}
