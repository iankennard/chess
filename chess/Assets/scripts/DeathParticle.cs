using UnityEngine;
using System.Collections;

public class DeathParticle : MonoBehaviour
{

    private GameObject _controlCenter;
    private GameObject _parentObject;
    ParticleState state;
    public float ttl;

    public void Start()
    {
        _controlCenter = GameObject.Find("gamecontrol");
        if (_controlCenter == null)
            throw new System.NullReferenceException("gamecontrol not found in DeathParticle");
        _parentObject = GameObject.Find("deathparticles");
        if (_parentObject == null)
            throw new System.NullReferenceException("deathparticles not found in DeathParticle");
        gameObject.tag = "deathparticles";
        GameObject[] tmp = GameObject.FindGameObjectsWithTag("deathparticles");
        if (tmp.Length == 1)
        {
            state = ParticleState.HIDDEN;
            renderer.enabled = false;
        }
        else
        {
            state = ParticleState.VISIBLE;
            renderer.enabled = true;
        }
        ttl = 2;
    }

    public void Update()
    {
        if (state == ParticleState.VISIBLE)
        {
            renderer.enabled = true;
            ttl -= Time.deltaTime;
            if (ttl <= 0)
                Destroy(gameObject);
            gameObject.GetComponent<ParticleSystem>().Play();
        }
    }

    public void SpawnParticle(Vector3 p)
    {
        if (_parentObject == null)
            throw new System.NullReferenceException("deathparticles not found in DeathParticle");
        Instantiate(_parentObject, p, Quaternion.identity);
    }

}