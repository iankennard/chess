using UnityEngine;
using System.Collections;

public class Background : MonoBehaviour {

    private GameObject _controlCenter;

    public void Start() {
        _controlCenter = GameObject.Find("gamecontrol");
        if (_controlCenter == null)
            throw new System.NullReferenceException("gamecontrol not found in Background");
    }

    public void OnMouseUpAsButton() {
        if (_controlCenter.GetComponent<GameManager>().gameState == GameState.BUSY) return;
        _controlCenter.GetComponent<GameManager>().selection = null;
    }

}
