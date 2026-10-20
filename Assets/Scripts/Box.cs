using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum RewardType
{
    Shield, Time
}


public class Box : MonoBehaviour
{


    private bool Activated = false;
    public RewardType Type = RewardType.Shield;
    Action<Player>[] Tests;

    void Start()
    {
        Tests = new Action<Player>[]
        {
            ForShape, ForStroop
        };
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if(!Activated && collision.gameObject.CompareTag("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    Player player = collision.collider.GetComponent<Player>();

                    Freeze(player, true);

                    int test = UnityEngine.Random.Range(0, Tests.Length);

                    Tests[test](player);

                    Activated = true;

                    break;
                }
            }
        }
    }    

    private void Freeze(Player player, bool freeze)
    {
        Rocket.Freeze(freeze);
        Enemy.Freeze = freeze;
        player?.Freeze(freeze);
    }

    private IEnumerator Unfreeze(float time)
    {
        yield return new WaitForSeconds(time);

        Freeze(null, false);
    }

    void ForShape(Player player)
    {
        SceneManager.LoadSceneAsync("Puzzle_ShapeSize", LoadSceneMode.Additive);

        ShapeSize.ClearFinishedEvents();
        ShapeSize.OnTestsFinished += (bool passed) =>
        {
            OnTestFinished(player, passed);
        };
    }

    void ForStroop(Player player)
    {
        SceneManager.LoadSceneAsync("Puzzle_StroopShape", LoadSceneMode.Additive);

        StroopShape.ClearFinishedEvents();
        StroopShape.OnTestsFinished += (bool passed) =>
        {
            OnTestFinished(player, passed);
        };
    }

    private void OnTestFinished(Player player, bool passed)
    {
        if(passed)
        {
            switch(Type)
            {
            case RewardType.Shield:
            {
                player.AddShield();
                Freeze(player, false);
            } break;
            case RewardType.Time:
            {
                Freeze(null, true);

                player.Freeze(false);

                StartCoroutine(Unfreeze(5.0f));
            } break;
            }
        }
        else
        {
            Freeze(player, false);
        }

    }

}
