using UnityEngine;


public abstract class Obstacle : Schedulable
{
    // Can this be defended against?
    public bool invulnerable = true;
    public float streakDamage = 2f;
    [SerializeField] public AudioClip collisionClip;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }


    // Update is called once per frame
    void Update()
    {


    }


    protected abstract void OnHit(Collision collision);
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


            if (hand != null) OnHit(collision);
        }


        // Check if we collided with the player
        Player player = other.GetComponent<Player>();
        if (player == null) return;

        // Decrease streak if we hit the player
        StreakTracker.Instance.streak -= streakDamage;

        if (collisionClip != null) AudioUtility.PlayClipAtPointWithVariation(collisionClip, collision.contacts[0].point, true);
    }
}
