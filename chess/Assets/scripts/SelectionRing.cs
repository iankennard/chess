using UnityEngine;
using System.Collections;

public enum SelectionRingState { VISIBLE, HIDDEN }

public class SelectionRing : MonoBehaviour
{

    public SelectionRingState selectionState;
    private GameObject _controlCenter;
    private GameObject _parentObject;
    public GameObject piece;
    public float opacity;
    public int count;
    public Material defaultMat;

    public void Start()
    {
        _controlCenter = GameObject.Find("gamecontrol");
        if (_controlCenter == null)
            throw new System.NullReferenceException("gamecontrol not found in SelectionRing");
        _parentObject = GameObject.Find("selectionring");
        if (_parentObject == null)
            throw new System.NullReferenceException("selectionring not found in SelectionRing");
        selectionState = SelectionRingState.HIDDEN;
        renderer.enabled = false;
        opacity = 1;
        count = 0;
        defaultMat = renderer.material;
    }

    public void Update()
    {
        if (_parentObject == null)
            throw new System.NullReferenceException("selectionring not found in SelectionRing");
        if (gameObject == _parentObject)
            return;

        if (selectionState == SelectionRingState.HIDDEN && _controlCenter.GetComponent<GameManager>().selection != null)
        {
            transform.position = _controlCenter.GetComponent<GameManager>().selection.transform.position + new Vector3(0, -3.95f, 0);
            selectionState = SelectionRingState.VISIBLE;
            renderer.enabled = true;
        }
        else
        {
            if (opacity <= 0)
                Destroy(gameObject);
            if (count == 8)
            {
                if (_controlCenter.GetComponent<GameManager>().gameState != GameState.BUSY && _controlCenter.GetComponent<GameManager>().selection != null)
                    Instantiate(gameObject, _controlCenter.GetComponent<GameManager>().selection.transform.position + new Vector3(0, -3.95f, 0), Quaternion.identity);
                //renderer.material.Lerp(defaultMat, GameObject.Find("dummyMatHolder").renderer.material, opacity);
            }
            opacity -= .02f;
            count++;
            transform.position += new Vector3(0, .05f, 0);
            renderer.material.SetColor("_Color", new Color(0, 1, 0, opacity));
            //renderer.material.SetFloat("_Cutoff", opacity);
        }
    }

    public void OnMouseUpAsButton()
    {
        if (_controlCenter.GetComponent<GameManager>().gameState == GameState.BUSY) return;
        _controlCenter.GetComponent<GameManager>().selection = null;
        _controlCenter.GetComponent<GameManager>().selectionChanged = true;
    }

    public void startSpawn()
    {
        if (_parentObject == null)
            throw new System.NullReferenceException("selectionring not found in SelectionRing");
        if (gameObject == _parentObject)
            Instantiate(gameObject, transform.position, Quaternion.identity);
    }

}
