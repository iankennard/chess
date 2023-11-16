using UnityEngine;
using System.Collections;

public class LightMovement : MonoBehaviour {

    private GameObject _controlCenter;

    public void Start() {
        _controlCenter = GameObject.Find("gamecontrol");
        if (_controlCenter == null)
            throw new System.NullReferenceException("gamecontrol not found in LightMovement");
    }

    public void Update() {
        transform.position = new Vector3(32 + 64 * Mathf.Cos(2 * Mathf.PI * Time.time / 90), 64, 32 + 64 * Mathf.Sin(2 * Mathf.PI * Time.time / 90));
        transform.LookAt(new Vector3(32, 0, 32));
    }

}
