using System.Collections;
using TMPro;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Detection")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float wallCheckDistance = 0.1f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Combat")]
    [SerializeField] private float stompBounceForce = 8f;

    public static bool Freeze = false;

    private Animator Anim;

    public LayerMask PlayerLayer;

    private Rigidbody2D rb;

    private bool IsSqaushed = false;
    private bool IsExploding = false;

    public bool facingRight
    {
        get
        {
            return Mathf.Sign(transform.localScale.x) == 1.0f ? true : false;
        }
    }
    private bool dead;

    public bool IsGrounded, IsWall;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Anim = GetComponent<Animator>();

        GetComponentsInChildren<Hurtbox>()[0].OnHurt.AddListener(Hit);
    }

    private void FixedUpdate()
    {
        if (dead)
            return;

        if(Freeze)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Move();
        CheckForTurn();
    }

    void Update()
    {
        Anim.SetBool("IsSquashed", IsSqaushed);
        Anim.SetBool("IsExploding", IsExploding);
        Anim.SetBool("IsIdle", !IsSqaushed && !IsExploding);
    }

    private void Move()
    {
        float direction = facingRight ? 1f : -1f;

        rb.linearVelocity = new Vector2(
            direction * moveSpeed,
            rb.linearVelocity.y
        );
    }

    private void CheckForTurn()
    {
        float direction = facingRight ? 1f : -1f;

        var v = Physics2D.Raycast(
            wallCheck.position,
            Vector2.right * direction,
            wallCheckDistance,
            groundLayer
        );
        // Check for wall
        IsWall = (v);

        // Check for ground in front of us
        IsGrounded = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        if (IsWall || !IsGrounded)
        {
            TurnAround();
        }
    }

    private void TurnAround()
    {
        //facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    // private void OnCollisionEnter2D(Collision2D collision)
    // {
    //     if (dead)
    //         return;

    //     if (!collision.gameObject.CompareTag("Player"))
    //         return;

    //     // Determine whether the player hit us from above.
    //     for (int i = 0; i < collision.contactCount; ++i)
    //     {
    //         ContactPoint2D contact = collision.GetContact(i);
    //         if (contact.normal.y < -0.5f)
    //         {
    //             Stomped(collision.gameObject);
    //             Debug.Log("Stomped");
    //             return;
    //         }
    //     }

    //     // Player touched us from the side.
    //     Debug.Log("Damaged");

    //     DamagePlayer(collision.gameObject);
    // }

    public void PlayerDetected(GameObject player)
    {
        if (dead)
            return;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        if (playerRb != null &&
            player.transform.position.y > transform.position.y)
        {
            Stomped(player);
        }
        else
        {
            DamagePlayer(player);
        }
    }

    public void Hit(Hitbox hitbox)
    {
        IsExploding = true;
        IsSqaushed = false;
        StartCoroutine(Die());
    }


    private void Stomped(GameObject player)
    {
        dead = true;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        if (playerRb != null)
        {
            playerRb.linearVelocity = new Vector2(
                playerRb.linearVelocity.x,
                stompBounceForce
            );
        }

        IsSqaushed = true;
        IsExploding = false;
        StartCoroutine(Die());
    }

    private void DamagePlayer(GameObject playerObj)
    {   
        Player player = playerObj.GetComponent<Player>();

        player.Damage();
        //PlayerHealth health = player.GetComponent<PlayerHealth>();
//
        //if (health != null)
        //{
        //    health.TakeDamage(1);
        //}
    }

    private IEnumerator Die()
    {
        // Stop movement.
        rb.linearVelocity = Vector2.zero;

        // GetComponent<Rigidbody2D>().excludeLayers |= PlayerLayer;

        dead = true;
        while(dead)
        {
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDeathAnimationFinished()
    {
        dead = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (wallCheck != null)
        {
            Gizmos.DrawLine(
                wallCheck.position,
                wallCheck.position +
                Vector3.right *
                (facingRight ? wallCheckDistance : -wallCheckDistance)
            );
        }

        if (groundCheck != null)
        {
            Gizmos.DrawLine(
                groundCheck.position,
                groundCheck.position + Vector3.down * groundCheckDistance
            );
        }
    }
}