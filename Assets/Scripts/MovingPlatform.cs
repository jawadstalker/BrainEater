using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EaseType
{
    Linear,
    EaseInQuad,
    EaseOutQuad,
    EaseInOutQuad,
    EaseInCubic,
    EaseOutCubic,
    EaseInOutCubic,
    EaseInSine,
    EaseOutSine,
    EaseInOutSine,
    SmoothStep,
    SmootherStep,
}

public class MovingPlatform : MonoBehaviour
{
    [Tooltip("The function used for interpolating.")]
    public EaseType Ease = EaseType.Linear;
    [Tooltip("The speed of interpolation.")]
    public float Speed = 10.0f;
    [Tooltip("Amount of time to wait before starting to move to the next Position.")]
    public float WaitTime = 0.0f;
    [Tooltip("Ping pong instead of looping.")]
    public bool TraceBack = true;

    [Tooltip("Positions to move to, Specified in deltas from the current position.")]
    public List<Vector2> Bounds;
    public int NextBound = 0;
    [SerializeField]
    private int LastOffset = -1;

    public float CurrentTime;
    public float MixVal;

    public Vector2 CurrentPos;

    private IEnumerator MovePlatform()
    {
        CurrentTime = 0.0f;

        while(true)
        {
            if(CurrentTime >= 1.0f)
            {
                if(TraceBack)
                {
                    if(NextBound == 0)
                    {
                        NextBound = 1;
                        LastOffset = -1;
                    }
                    else if(NextBound == Bounds.Count - 1)
                    {
                        NextBound = Bounds.Count - 2;
                        LastOffset = 1;
                    }
                    else
                    {
                        NextBound -= LastOffset;
                    }
                }
                else
                {
                    NextBound = (NextBound + 1) % (Bounds.Count);
                }

                CurrentTime = 0.0f;

                if(WaitTime > 0.0f)
                {
                    yield return new WaitForSeconds(WaitTime);
                }
            }

            int fromIndex = NextBound + LastOffset;
            
            if(fromIndex < 0)
            {
                fromIndex = Bounds.Count - 1;    
            }

            int toIndex = NextBound;

            float len = (Vector2.Distance(Bounds[fromIndex], Bounds[toIndex]));

            float step = len > 0.0001f ? (Speed * Time.deltaTime) / len : 1.0f;

            CurrentTime += step;

            MixVal = ApplyEase(CurrentTime);
            CurrentPos = Vector2.Lerp(Bounds[fromIndex], Bounds[toIndex], MixVal);
            transform.localPosition = new Vector3(CurrentPos.x, CurrentPos.y, transform.localPosition.z);
            yield return null;
        }
    }

    void Start()
    {
        Bounds.Insert(0, transform.localPosition);

        for(int i = 1; i < Bounds.Count; ++i)
        {
            Bounds[i] += (Vector2)transform.localPosition;
        }

        NextBound = 1;

        if(Bounds.Count > 1)
        {
            StartCoroutine(MovePlatform());
        }
        else
        {
            Debug.LogWarning($"{name}: Moving platform needs at least one position to move to");
        }
    }

    private float ApplyEase(float t)
    {
        t = Mathf.Clamp01(t);
        switch (Ease)
        {
            case EaseType.EaseInQuad:     return t * t;
            case EaseType.EaseOutQuad:    return 1f - (1f - t) * (1f - t);
            case EaseType.EaseInOutQuad:  return t < 0.5f
                ? 2f * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            case EaseType.EaseInCubic:    return t * t * t;
            case EaseType.EaseOutCubic:   return 1f - Mathf.Pow(1f - t, 3f);
            case EaseType.EaseInOutCubic: return t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

            case EaseType.EaseInSine:     return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            case EaseType.EaseOutSine:    return Mathf.Sin(t * Mathf.PI * 0.5f);
            case EaseType.EaseInOutSine:  return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

            case EaseType.SmoothStep:     return t * t * (3f - 2f * t);
            case EaseType.SmootherStep:   return t * t * t * (t * (t * 6f - 15f) + 10f);

            default:                      return t;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            for(int i = 0; i < collision.contactCount; ++i)
            {
                if (collision.GetContact(i).normal.y < -0.5f)
                {
                    //collision.gameObject.GetComponent<Player>().CurrentPlatform = this;
                    collision.transform.SetParent(transform);
                    break;
                }
            }
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            //collision.gameObject.GetComponent<Player>().CurrentPlatform = null;
            collision.transform.SetParent(null);
        }
    }
}
