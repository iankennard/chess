using UnityEngine;
using System.Collections;

public class Highlight : MonoBehaviour {

    private GameObject _controlCenter;
    private GameObject _parentObject;
    public int x;
    public int y;
    bool over;

    public void Start() {
        _controlCenter = GameObject.Find("gamecontrol");
        if (_controlCenter == null)
            throw new System.NullReferenceException("gamecontrol not found in Highlight");
        _parentObject = GameObject.Find("squarehighlight");
        if (_parentObject == null)
            throw new System.NullReferenceException("squarehighlight not found in Highlight");
        over = false;
    }

    public void Update() {
        if (_controlCenter.GetComponent<GameManager>().gameState == GameState.BUSY)
        {
            if (_parentObject == null)
                throw new System.NullReferenceException("squarehighlight not found in Highlight");
            if (gameObject != _parentObject)
                Destroy(gameObject);
            return;
        }
        /*
        if (_controlCenter.GetComponent<GameManager>().selection == null)
            Destroy(gameObject);
        */
        if(over)
            renderer.material.SetFloat("_Cutoff", Mathf.PingPong(Time.time, 1));
        else
            renderer.material.SetFloat("_Cutoff", .9f + Mathf.PingPong(Time.time, .1f));
    }

    public void OnMouseEnter()
    {
        over = true;
    }

    public void OnMouseExit()
    {
        over = false;
    }

    public void OnMouseUpAsButton() {
        if (_controlCenter.GetComponent<GameManager>().gameState == GameState.BUSY) return;
        _controlCenter.GetComponent<GameManager>().move.move(x, y);
    }

}