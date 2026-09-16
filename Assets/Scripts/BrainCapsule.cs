using UnityEngine;

public class BrainCapsule : MonoBehaviour
{
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            if(LevelTransition.Instance == null)
            {
                Debug.Log("Couldn't find a level transition in the scene.");
                return;
            }

            LevelTransition.Instance.CanTransition = true;

            Destroy(gameObject);
        }
    }
}
