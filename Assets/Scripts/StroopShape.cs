using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StroopShape : Puzzle
{
    [System.Serializable]
    public struct NamedColor
    {
        public string PersianName;
        public Color Color;
    }

    public NamedColor[] Colors = new NamedColor[]
    {
        new NamedColor { PersianName = "قرمز",   Color = new Color32(229, 57, 53, 255) },  // Red
        new NamedColor { PersianName = "آبی",    Color = new Color32(30, 136, 229, 255) }, // Blue
        new NamedColor { PersianName = "سبز",    Color = new Color32(67, 160, 71, 255) },  // Green
        new NamedColor { PersianName = "زرد",    Color = new Color32(253, 216, 53, 255) }, // Yellow
        new NamedColor { PersianName = "نارنجی", Color = new Color32(251, 140, 0, 255) },  // Orange
        new NamedColor { PersianName = "بنفش",   Color = new Color32(142, 36, 170, 255) }, // Purple
        new NamedColor { PersianName = "صورتی",  Color = new Color32(236, 64, 122, 255) }, // Pink
        new NamedColor { PersianName = "قهوه‌ای", Color= new Color32(109, 76, 65, 255) },  // Brown
        new NamedColor { PersianName = "مشکی",   Color = new Color32(33, 33, 33, 255) },    // Black
        new NamedColor { PersianName = "سفید",   Color = new Color32(255, 255, 255, 255) }, // White
    };

    [Serializable]
    public class GridCell
    {
        public RectTransform Cell;
        public Shape Shape;
        public NamedColor Color;

        public void Set(NamedColor color, Shape shape)
        {
            Shape = shape;
            
            Color = color;

            shape.Img.rectTransform.SetParent(Cell, false);

            shape.Img.rectTransform.anchorMin = Vector2.zero;   // (0, 0)
            shape.Img.rectTransform.anchorMax = Vector2.one;    // (1, 1)
            shape.Img.rectTransform.offsetMin = Vector2.zero;   // Left, Bottom = 0
            shape.Img.rectTransform.offsetMax = Vector2.zero;   // Right, Top = 0 (offsetMax is measured inward, so 0 means flush)

            Shape.Img.color = color.Color;
        }
    }

    [Serializable]
    public class Shape
    {
        public Image Img;
        public string Name;
    }

    public Shape[] Shapes;

    public RTLTMPro.RTLTextMeshPro HeaderText;

    public static event Action<bool> OnTestsFinished;

    public int RightAnswer;

    public static void ClearFinishedEvents()
    {
        OnTestsFinished = null;
    }

    public GridCell[] Cells = new GridCell[4];

    void Start()
    {
        StartCoroutine(NextTest(true));
    }

    private IEnumerator NextTest(bool firstRun)
    {
        if(CorrectCount == CorrectToPass || IncorrectCount == IncorrectToFail)
        {
            OnTestsFinished?.Invoke(CorrectCount == CorrectToPass);
            SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("Puzzle_StroopShape"));
        }
        else
        {
            foreach(var c in Cells)
            {
                c.Cell.gameObject.SetActive(false);
            }
            
            if(!firstRun)
            {
                yield return new WaitForSecondsRealtime(TimeBetweenTests);
            }

            List<int> cols = ChooseNUnique(4, Colors.Length - 1);

            List<int> shapes = ChooseNUnique(4, Shapes.Length - 1);

            int i = 0;
            foreach(var c in Cells)
            {
                c.Set(Colors[cols[i]], Shapes[shapes[i]]);
                c.Cell.gameObject.SetActive(true);
                ++i;
            }

            RightAnswer = UnityEngine.Random.Range(0, 4);

            HeaderText.text = $"{Cells[RightAnswer].Shape.Name} {Cells[RightAnswer].Color.PersianName} رو انتخاب کن.";
        }
    }

    public void OnShapeClicked(GameObject clicked)
    {
        bool correct = false;

        for(int i = 0; i < Cells.Length; ++i)
        {
            if(Cells[i].Shape.Img.gameObject == clicked)
            {
                if(RightAnswer == i)
                {
                    CorrectCount++;
                    correct = true;
                    break;
                }
            }
        }

        if(!correct)
        {
            IncorrectCount++;
        }

        StartCoroutine(NextTest(false));
    }

    public static List<int> ChooseNUnique(int n, int m)
    {
        // Fisher-Yates partial shuffle - fast and no duplicates
        List<int> pool = Enumerable.Range(0, m + 1).ToList();
        for (int i = 0; i < n; i++)
        {
            int j = UnityEngine.Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        return pool.GetRange(0, n);
    }

}
