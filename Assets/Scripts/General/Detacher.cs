using System.Collections.Generic;
using UnityEngine;

public class Detacher : MonoBehaviour
{

    [SerializeField] private List<GameObject> _gameObjects;
    [SerializeField] private List<ParticleSystem> _particleSystems;

    [SerializeField] private List<TrailRenderer> _trails;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Detach()
    {
        foreach (var particleSystem in _particleSystems)
        {
            // Stop if already playing and looping and destroy when complete
            // We check looping because it will stop naturally if looping is not enabled.
            // Otherwise we need to manually stop it or it will continue
            if (particleSystem.isPlaying && particleSystem.main.loop)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            DetachGameObject(particleSystem.gameObject, particleSystem.main.startLifetime.constantMax);
        }

        foreach (var trail in _trails)
        {
            DetachGameObject(trail.gameObject, trail.time);
        }

        foreach (var go in _gameObjects)
        {
            DetachGameObject(go, -1); // GameObjects are responsible for destroying themselves
        }
    }

    void DetachGameObject(GameObject obj, float destroyAfter)
    {
        if (obj == gameObject) return;

        obj.transform.SetParent(null, true);

        if (destroyAfter > 0) Destroy(obj, destroyAfter);
    }
}
