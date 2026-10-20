using System.Collections.Generic;
using UnityEngine;

public class Rocket : MonoBehaviour
{
    public bool AutoStart = false;
    private Rigidbody2D Rb;

    public float MoveSpeed = 5.0f;
    public static List<Rocket> Rockets = new List<Rocket>();

    private bool WasMoving = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Rb = GetComponentInParent<Rigidbody2D>();
        if(AutoStart)
        {
            StartMoving();
        }

        Rockets.Add(this);
    }

    void OnDestroy()
    {
        Rockets.Remove(this);
    }

    public void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider.gameObject.CompareTag("Player"))
        {
            Debug.Log("Target");
            collider.GetComponent<Player>().Damage();
        }
    }

    public static void Freeze(bool freeze)
    {
        foreach(var rock in Rockets)
        {
            if(freeze)
            {
                rock.WasMoving = rock.Rb.linearVelocity.sqrMagnitude != 0;
                rock.Rb.linearVelocity = Vector2.zero;
            }
            else if(rock.WasMoving)
            { 
                rock.StartMoving();
            }
        }
    }

    public void StartMoving()
    {
        Rb.linearVelocity = -transform.right * MoveSpeed;
    }
}
