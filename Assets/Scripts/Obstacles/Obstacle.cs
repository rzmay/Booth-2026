using UnityEngine;


public abstract class Obstacle : Schedulable
{
    // Can this be defended against?
    public bool invulnerable = true;
    public float streakDamage = 2f;
    [SerializeField] public AudioClip collisionClip;

    protected abstract void OnHitHand(Collision collision);
    protected abstract void OnHitPlayer(Collision collission);
    // Implementation is up to the subclass


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


            if (hand != null)
            {
                OnHitHand(collision);
                return;
            }
        }


        // Check if we collided with the player
        Player player = other.GetComponent<Player>();
        if (player == null) return;

        // Decrease streak if we hit the player
        StreakTracker.Instance.streak -= streakDamage;

        if (collisionClip != null && player.obstacleAudioSource != null)
        {
            player.obstacleAudioSource.clip = collisionClip;
            player.obstacleAudioSource.Play();
        }

        OnHitPlayer(collision);
    }
}
