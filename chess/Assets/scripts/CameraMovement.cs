using UnityEngine;
using System.Collections;

public enum CameraState { STILL, FOLLOWPIECE, ROTATEAROUNDBOARD, ADJUSTINGANGLE }

public class CameraMovement : MonoBehaviour {

    private GameObject _controlCenter;
    public bool rotate;
    public int smooth;
    public float interp;
    public CameraState cameraState;
    public GameObject movingPiece;

    public void Start() {
        _controlCenter = GameObject.Find("gamecontrol");
        if (_controlCenter == null)
            throw new System.NullReferenceException("gamecontrol not found in CameraMovement");
        smooth = 5;
        interp = 0;
        cameraState = CameraState.STILL;
        movingPiece = null;
    }

    public void Update() {
        if (cameraState == CameraState.FOLLOWPIECE)
        {
            if (!_controlCenter.GetComponent<GameManager>().singlePlayer)
            {
                cameraState = CameraState.STILL;
                _controlCenter.GetComponent<GameManager>().cameraProcessing = false;
                _controlCenter.GetComponent<GameManager>().done = true;
            }
            else
            {
                if (movingPiece != null)
                {
                    transform.position = Vector3.Lerp(transform.position, movingPiece.transform.position + Vector3.Scale(new Vector3(16, 16, 16), Vector3.Cross(movingPiece.transform.up, movingPiece.transform.forward)), Time.deltaTime * .5f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movingPiece.transform.position - transform.position), Time.deltaTime * 1.5f);
                }
                else
                {
                    if (!_controlCenter.GetComponent<GameManager>().moveAnimation)
                    {
                        interp = 0;
                        cameraState = CameraState.ROTATEAROUNDBOARD;
                    }
                }
            }
        }
        if (cameraState == CameraState.ROTATEAROUNDBOARD)
        {
            if (_controlCenter.GetComponent<GameManager>().turn == PieceColor.BLACK)
            {
                //transform.position = Vector3.Lerp(transform.position, new Vector3(32, 60, -32), Time.deltaTime * 4);
                //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(45, 0, 0), Time.deltaTime * 8);
                interp += Time.deltaTime * .5f;
                if (interp > 1)
                    interp = 1;
                transform.position = Vector3.Lerp(transform.position, new Vector3((interp - 1) * 64 + 32 - (2 - interp) * 64 * interp * Mathf.Sin(Mathf.PI * interp), 60, (1 - interp) * -64 - 32 + 64 * interp - (2 - interp) * 64 * interp * Mathf.Cos(Mathf.PI * interp)), Time.deltaTime * .5f * (1+6*interp));
                //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(45, 180 * interp, 0), Time.deltaTime / 1.5f);
                Quaternion temp = transform.rotation;
                transform.LookAt(new Vector3(32, 0, 32));
                Quaternion temp2 = transform.rotation;
                transform.rotation = temp;
                transform.rotation = Quaternion.Slerp(transform.rotation, temp2, Time.deltaTime * 4);
                if (Vector3.Distance(transform.position, new Vector3(32, 60, 96)) < .05f)
                {
                    transform.position = new Vector3(32, 60, 96);
                    //transform.LookAt(new Vector3(32, 0, 32));
                    cameraState = CameraState.ADJUSTINGANGLE;
                }
            }
            else
            {
                //transform.position = Vector3.Lerp(transform.position, new Vector3(32, 60, 96), Time.deltaTime * 4);
                //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(45, 180, 0), Time.deltaTime * 8);
                interp += Time.deltaTime * .5f;
                if (interp > 1)
                    interp = 1;
                transform.position = Vector3.Lerp(transform.position, new Vector3((1-interp) * 64 + 32 + (2 - interp) * 64 * interp * Mathf.Sin(Mathf.PI * interp), 60, (1 - interp) * 64 + 96 - 64 * interp + (2 - interp) * 64 * interp * Mathf.Cos(Mathf.PI * interp)), Time.deltaTime * .5f * (1 + 6 * interp));
                //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(45, 180 * (1 - interp), 0), Time.deltaTime / 1.5f);
                Quaternion temp = transform.rotation;
                transform.LookAt(new Vector3(32, 0, 32));
                Quaternion temp2 = transform.rotation;
                transform.rotation = temp;
                transform.rotation = Quaternion.Slerp(transform.rotation, temp2, Time.deltaTime * 4);
                if (Vector3.Distance(transform.position, new Vector3(32, 60, -32)) < .05f)
                {
                    transform.position = new Vector3(32, 60, -32);
                    //transform.LookAt(new Vector3(32, 0, 32));
                    cameraState = CameraState.ADJUSTINGANGLE;
                }
            }
        }
        if (cameraState == CameraState.ADJUSTINGANGLE)
        {
            if (_controlCenter.GetComponent<GameManager>().turn == PieceColor.BLACK)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(45, 180, 0), Time.deltaTime * 16);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(45, 0, 0), Time.deltaTime * 16);
            if (Quaternion.Angle(transform.rotation, Quaternion.Euler(45, 0, 0)) < .01f || Quaternion.Angle(transform.rotation, Quaternion.Euler(45, 180, 0)) < .01f)
            {
                cameraState = CameraState.STILL;
                _controlCenter.GetComponent<GameManager>().cameraProcessing = false;
                _controlCenter.GetComponent<GameManager>().done = true;
            }
        }
    }

}
