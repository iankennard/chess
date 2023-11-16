using UnityEngine;
using System.Collections;

public enum ParticleState { VISIBLE, HIDDEN }

public class MovementParticle : MonoBehaviour
{

    private GameObject _controlCenter;
    private GameObject _parentObject;
    ParticleState state;
    public float ttl;

    public void Start()
    {
        _controlCenter = GameObject.Find("gamecontrol");
        if (_controlCenter == null)
            throw new System.NullReferenceException("gamecontrol not found in MovementParticle");
        _parentObject = GameObject.Find("movementparticles");
        if (_parentObject == null)
            throw new System.NullReferenceException("movementparticles not found in MovementParticle");
        gameObject.tag = "movementparticles";
        GameObject[] tmp = GameObject.FindGameObjectsWithTag("movementparticles");
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
        ttl = 3.9f;
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
            throw new System.NullReferenceException("movementparticles not found in MovementParticle");
        Instantiate(_parentObject, p, Quaternion.identity);
    }

}