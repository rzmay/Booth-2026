using UnityEngine;

public abstract class Obstacle : MonoBehaviour
{
    // Can this be defended against?
    public bool invulnerable = true;
    public float streakDamage = 2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnHit(Collision collision)
    {
        // Implementation is up to the subclass
    }

    /* Whenever an obstacle collides with a player's body, deal streak damage
    * If the obstacle collides with a player's hand and is not invulnerable, trigger OnHit
    */
    private void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;

        // If defendable, check if hit
        if (!invulnerable)
        {
            Hand hand = other.GetComponent<Hand>();

            if (hand != null) OnHit(collision);
        }

        Player player = other.GetComponent<Player>();

        // Decrease streak if we hit the player
        if (player != null) StreakTracker.Instance.streak -= streakDamage;
    }
}
