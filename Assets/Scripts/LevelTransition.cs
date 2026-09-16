using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelTransition : MonoBehaviour
{

    public bool CanTransition = false;

    static public LevelTransition Instance;

    public LevelTransitions Transitions;

    void Start()
    {
        Instance = this;
        CanTransition = false;
    }

    void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }

    private IEnumerator Wait(float time)
    {
        yield return new WaitForSeconds(time);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Player") && CanTransition)
        {
            for(int i = 0; i < collision.contactCount; ++i)
            {
                if((collision.GetContact(i).normal.y < -0.5f) || (collision.GetContact(i).normal.y > 0.5f))
                {
                    GetComponent<Collider2D>().enabled = false;

                    collision.gameObject.GetComponent<PlayerInput>().enabled = false;

                    float direction = collision.GetContact(i).normal.y < -0.5f ? 1.0f : -1.0f;

                    collision.rigidbody.gravityScale = direction * 10.0f;

                    collision.gameObject.GetComponent<Player>().LoadLevel(
                        Transitions.GetNextScene(), Wait(1.5f)
                    );
                    return;
                }
            }
        }    
    }
}
