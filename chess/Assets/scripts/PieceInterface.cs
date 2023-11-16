using UnityEngine;
using System.Collections;

public enum MoveState { NONE, UP, ACROSS, DOWN, FADEOUT, FADEIN }

public class PieceInterface : MonoBehaviour {

    private GameObject _controlCenter;
    public GameObject movementParticles;
    public GameObject parentDeathParticle;
    float pt;
    public PieceInfo pieceInfo;
    public bool moving;
    public float movex;
    public float movey;
    public MoveState moveState;
    public int squareSize;
    public int pitch;
    public Vector3 target;
    public Vector3 direction;
    public Quaternion targetrot;
    public bool first;
    public float dist;
    public Vector3 deathbed;
    public bool twitch;
    public Vector3 twitchdest;

    public Camera mainCamera;
    public GameObject gameLight;

    public Vector3 pdiff;
    public Vector3 plast;

    public int x;
    public int y; // stupid hack to get unity to show piece info
    public bool al;

    public void Start() {
        _controlCenter = GameObject.Find("gamecontrol");
        if (_controlCenter == null)
            throw new System.NullReferenceException("gamecontrol not found in PieceInterface");
        movementParticles = GameObject.Find("movementparticles");
        if (movementParticles == null)
            throw new System.NullReferenceException("movementparticles not found in PieceInterface");
        parentDeathParticle = GameObject.Find("deathparticles");
        if (parentDeathParticle == null)
            throw new System.NullReferenceException("deathparticles not found in PieceInterface");
        moving = false;
        pitch = 0;
        target = new Vector3(0, 0, 0);
        direction = new Vector3(0, 0, 0);
        targetrot = Quaternion.identity;
        dist = 0;
        first = true;
        deathbed = new Vector3(-128, -128, -128);
        twitch = false;
        twitchdest = new Vector3(0, 0, 0);

        mainCamera = Camera.main;
        if (mainCamera == null)
            throw new System.NullReferenceException("no camera");

        gameLight = GameObject.Find("Directional light");
        if (gameLight == null)
            throw new System.NullReferenceException("Directional light not found in PieceInterface");

        pdiff = Vector3.zero;
        plast = movementParticles.transform.position;

        x = pieceInfo.x; y = pieceInfo.y; al = pieceInfo.alive;

        movex = -1; movey = -1;
        moveState = MoveState.NONE;
        squareSize = _controlCenter.GetComponent<GameManager>().squareSize;
        renderer.material.shader = Shader.Find("Transparent/Cutout/Soft Edge Unlit");
        //renderer.material.shader = Shader.Find("Diffuse");
    }

    public void OnMouseEnter() {
        if(pieceInfo.alive && _controlCenter.GetComponent<GameManager>().selection != gameObject) {
            if (_controlCenter.GetComponent<GameManager>().turn == pieceInfo.color)
                    renderer.material.shader = Shader.Find("Unlit/Additive Colored");
            else
            {
                foreach (GameObject h in _controlCenter.GetComponent<GameManager>().highlighted)
                {
                    if (h.GetComponent<Highlight>().x == pieceInfo.x && h.GetComponent<Highlight>().y == pieceInfo.y)
                        renderer.material.shader = Shader.Find("Unlit/Additive Colored");
                }
            }
        }
    }

    public void OnMouseExit() {
        renderer.material.shader = Shader.Find("Transparent/Cutout/Soft Edge Unlit");
        //renderer.material.shader = Shader.Find("Diffuse");
    }

    public void OnMouseUpAsButton() {
        if (_controlCenter.GetComponent<GameManager>().gameState == GameState.BUSY) return;
        if (pieceInfo.alive)
        {
            if (_controlCenter.GetComponent<GameManager>().singlePlayer)
            {
                if (_controlCenter.GetComponent<GameManager>().turn == pieceInfo.color)
                {
                    if (_controlCenter.GetComponent<GameManager>().selection != gameObject)
                    {
                        _controlCenter.GetComponent<GameManager>().selection = gameObject;
                        _controlCenter.GetComponent<GameManager>().selectionChanged = true;
                        renderer.material.shader = Shader.Find("Transparent/Cutout/Soft Edge Unlit");
                        //renderer.material.shader = Shader.Find("Diffuse");
                    }
                    else
                    {
                        _controlCenter.GetComponent<GameManager>().selection = null;
                        _controlCenter.GetComponent<GameManager>().selectionChanged = true;
                        renderer.material.shader = Shader.Find("Unlit/Additive Colored");
                    }
                }
                else
                {
                    foreach (GameObject h in _controlCenter.GetComponent<GameManager>().highlighted)
                    {
                        if (h.GetComponent<Highlight>().x == pieceInfo.x && h.GetComponent<Highlight>().y == pieceInfo.y)
                        {
                            _controlCenter.GetComponent<GameManager>().move.x = pieceInfo.x;
                            _controlCenter.GetComponent<GameManager>().move.y = pieceInfo.y;
                            _controlCenter.GetComponent<GameManager>().move.entered = true;
                            renderer.material.shader = Shader.Find("Unlit/Additive Colored");
                            break;
                        }
                    }
                }

            }

            else
            {
                if (_controlCenter.GetComponent<GameManager>().multiOwnColor == pieceInfo.color)
                {
                    if (_controlCenter.GetComponent<GameManager>().selection != gameObject)
                    {
                        _controlCenter.GetComponent<GameManager>().selection = gameObject;
                        _controlCenter.GetComponent<GameManager>().selectionChanged = true;
                        renderer.material.shader = Shader.Find("Transparent/Cutout/Soft Edge Unlit");
                        //renderer.material.shader = Shader.Find("Diffuse");
                    }
                    else
                    {
                        _controlCenter.GetComponent<GameManager>().selection = null;
                        _controlCenter.GetComponent<GameManager>().selectionChanged = true;
                        renderer.material.shader = Shader.Find("Unlit/Additive Colored");
                    }
                }
                else
                {
                    foreach (GameObject h in _controlCenter.GetComponent<GameManager>().highlighted)
                    {
                        if (h.GetComponent<Highlight>().x == pieceInfo.x && h.GetComponent<Highlight>().y == pieceInfo.y)
                        {
                            _controlCenter.GetComponent<GameManager>().move.x = pieceInfo.x;
                            _controlCenter.GetComponent<GameManager>().move.y = pieceInfo.y;
                            _controlCenter.GetComponent<GameManager>().move.entered = true;
                            renderer.material.shader = Shader.Find("Unlit/Additive Colored");
                            break;
                        }
                    }
                }

            }
        }
    }

    public void Update()
    {
        x = pieceInfo.x; y = pieceInfo.y; al = pieceInfo.alive;

        // shadows (projectors)
        foreach (Transform child in transform)
            child.transform.LookAt((transform.position - gameLight.transform.position) + transform.position);

        if (moving)
        {
            if (pieceInfo.alive)
            {
                mainCamera.GetComponent<CameraMovement>().movingPiece = gameObject;

                movementParticles.renderer.enabled = true;
                movementParticles.transform.position = transform.position;
                //movementParticles.GetComponent<ParticleSystem>().startSpeed = 0;
                if (first)
                    plast = transform.position;
                movementParticles.GetComponent<ParticleSystem>().Play();

                pdiff = movementParticles.transform.position - plast;
                plast = movementParticles.transform.position;
                //pdiff = new Vector3(pdiff.x, pdiff.y, pdiff.z);

                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[1000];
                int len = movementParticles.GetComponent<ParticleSystem>().GetParticles(particles);
                for (int i = 0; i < len; i++)
                    particles[i].position -= pdiff;
                movementParticles.GetComponent<ParticleSystem>().SetParticles(particles, len);

                if (moveState == MoveState.UP)
                {
                    if (first)
                    {
                        direction = new Vector3(movex - transform.position.x, 0, movey - transform.position.z);
                        direction.Normalize();
                        dist = Vector3.Distance(transform.position, new Vector3(movex, 5, movey));
                        target = new Vector3(transform.position.x + direction.x, 16, transform.position.z + direction.z);
                        targetrot = Quaternion.AngleAxis(30, Vector3.Cross(Vector3.up, direction)) * transform.rotation;
                        first = false;

                    }
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetrot, Time.deltaTime * 2);
                    transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 2);

                    if (Vector3.Distance(transform.position, target) < 2)
                    {
                        moveState = MoveState.ACROSS;
                        first = true;
                    }
                }
                if (moveState == MoveState.ACROSS)
                {
                    if (first)
                    {
                        target = new Vector3(movex - direction.x, 16, movey - direction.z);
                        targetrot = Quaternion.AngleAxis(0, Vector3.Cross(Vector3.up, direction)) * transform.rotation;
                        first = false;
                    }
                    transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 64 / dist);

                    if (Vector3.Distance(transform.position, target) < 2)
                    {
                        moveState = MoveState.DOWN;
                        first = true;
                    }
                }
                if (moveState == MoveState.DOWN)
                {
                    if (first)
                    {
                        target = new Vector3(movex, 5, movey);
                        targetrot = Quaternion.AngleAxis(-30, Vector3.Cross(Vector3.up, direction)) * transform.rotation;
                        first = false;

                    }
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetrot, Time.deltaTime * 2);
                    transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 2);

                    if (Vector3.Distance(transform.position, target) < .1f)
                    {
                        // sound stuff
                        audio.clip = GameObject.Find("sndTap"+System.Convert.ToString((int)(Random.value*2.99f+1))).audio.clip;
                        audio.Play();

                        transform.position = target;
                        if (pieceInfo.color == PieceColor.BLACK)
                            transform.rotation = Quaternion.AngleAxis(0, Vector3.up);
                        if (pieceInfo.color == PieceColor.WHITE)
                            transform.rotation = Quaternion.AngleAxis(180, Vector3.up);
                        first = true;
                        moveState = MoveState.NONE;
                        moving = false;
                        pieceInfo.hasMoved = true;
                        if (_controlCenter.GetComponent<GameManager>().castle)
                        {
                            mainCamera.GetComponent<CameraMovement>().movingPiece = _controlCenter.GetComponent<GameManager>().castleRook;

                            particles = new ParticleSystem.Particle[1000];
                            len = movementParticles.GetComponent<ParticleSystem>().GetParticles(particles);
                            for (int i = 0; i < len; i++)
                                particles[i].position -= _controlCenter.GetComponent<GameManager>().castleRook.transform.position - movementParticles.transform.position;
                            movementParticles.GetComponent<ParticleSystem>().SetParticles(particles, len);
                            _controlCenter.GetComponent<GameManager>().castleRook.GetComponent<PieceInterface>().MovePiece(_controlCenter.GetComponent<GameManager>().castleRookX, _controlCenter.GetComponent<GameManager>().castleRookY);
                            _controlCenter.GetComponent<GameManager>().busyUpdate = true;
                            _controlCenter.GetComponent<GameManager>().castle = false;
                        }
                        else
                        {
                            movementParticles.GetComponent<ParticleSystem>().Stop();
                            bool done = true;
                            foreach (GameObject g in _controlCenter.GetComponent<GameManager>().pieces)
                            {
                                if (g.GetComponent<PieceInterface>().moving && g.GetComponent<PieceInterface>().pieceInfo.alive)
                                {
                                    mainCamera.GetComponent<CameraMovement>().movingPiece = g;
                                    done = false;
                                    break;
                                }
                            }
                            if (done)
                            {
                                mainCamera.GetComponent<CameraMovement>().movingPiece = null;
                                _controlCenter.GetComponent<GameManager>().moveAnimation = false;
                            }
                            if (pieceInfo.type == PieceType.P && ((pieceInfo.color == PieceColor.WHITE && pieceInfo.y == 7) || (pieceInfo.color == PieceColor.BLACK && pieceInfo.y == 0)))
                                _controlCenter.GetComponent<GameManager>().gameState = GameState.PROMOTION;
                            else
                            {
                                if (_controlCenter.GetComponent<GameManager>().fiftyMoveCounter >= 50 || _controlCenter.GetComponent<GameManager>().moveRepetition >= 3)
                                    _controlCenter.GetComponent<GameManager>().gameState = GameState.DRAWPOSSIBLE;
                                else
                                {
                                    if(_controlCenter.GetComponent<GameManager>().moveAnimation == false) {
                                        _controlCenter.GetComponent<GameManager>().cameraProcessing = true;
                                        _controlCenter.GetComponent<GameManager>().busyUpdate = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (moveState == MoveState.FADEOUT)
                {
                    if (first)
                    {
                        renderer.material.shader = Shader.Find("Transparent/Cutout/Soft Edge Unlit");
                        //renderer.material.shader = Shader.Find("Diffuse");
                        parentDeathParticle.GetComponent<DeathParticle>().SpawnParticle(transform.position);
                        renderer.material.color = new Color(1, 1, 1, 1);
                        first = false;
                    }
                    renderer.material.color = new Color(1, 1, 1, renderer.material.color.a - Time.deltaTime * .5f);

                    if(twitch) {
                        if(Vector3.Distance(transform.position, twitchdest) < .1f && twitchdest.y < transform.position.y)
                            twitchdest = new Vector3(twitchdest.x, twitchdest.y+.4f, twitchdest.z);
                        if(Vector3.Distance(transform.position, twitchdest) < .1f && twitchdest.y > transform.position.y)
                            twitch = false;
                        transform.position = Vector3.Lerp(transform.position, twitchdest, Time.deltaTime*16);
                        if (pieceInfo.color == PieceColor.BLACK)
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(30 + Random.Range(-15, 15), 0, 0), Time.deltaTime * 16);
                        else
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(30 + Random.Range(-15, 15), 180, 0), Time.deltaTime * 16);
                    }
                    else {
                        transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, 10, transform.position.z), Time.deltaTime*2);
                        if (transform.position.y - Mathf.Floor(transform.position.y) < .1f && transform.position.y > 7)
                        {
                            twitch = true;
                            twitchdest = new Vector3(transform.position.x, transform.position.y - .3f, transform.position.z);
                        }
                        if (pieceInfo.color == PieceColor.BLACK)
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(30, 0, 0), Time.deltaTime);
                        else
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(30, 180, 0), Time.deltaTime);
                    }

                    if (renderer.material.color.a <= 0)
                    {
                        moveState = MoveState.FADEIN;
                        first = true;
                    }
                }
                if (moveState == MoveState.FADEIN)
                {
                    if (first)
                    {
                        transform.position = deathbed;
                        transform.rotation = Quaternion.Inverse(transform.rotation);
                        parentDeathParticle.GetComponent<DeathParticle>().SpawnParticle(transform.position);
                        first = false;
                    }
                    renderer.material.color = new Color(1, 1, 1, renderer.material.color.a + Time.deltaTime * .5f);

                    if (pieceInfo.color == PieceColor.BLACK)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, 0), Time.deltaTime);
                    else
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 180, 0), Time.deltaTime);

                    if (renderer.material.color.a >= 1)
                    {
                        first = true;
                        moveState = MoveState.NONE;
                        moving = false;
                        bool done = true;
                        foreach (GameObject g in _controlCenter.GetComponent<GameManager>().pieces)
                        {
                            if (g.GetComponent<PieceInterface>().moving) {
                                done = false;
                                break;
                            }
                        }
                        if (done)
                        {
                            _controlCenter.GetComponent<GameManager>().moveAnimation = false;
                            _controlCenter.GetComponent<GameManager>().cameraProcessing = true;
                            _controlCenter.GetComponent<GameManager>().busyUpdate = true;
                        }
                    }
                }
            }
        }
    }

    public void MovePiece(int x, int y)
    {
        moving = true;
        movex = squareSize * (x + .5f);
        movey = squareSize * (y + .5f);
        moveState = MoveState.UP;
        _controlCenter.GetComponent<GameManager>().moveAnimation = true;
    }

    public void Die(Vector3 p)
    {
        moving = true;
        pieceInfo.alive = false;
        pieceInfo.x = -1;
        pieceInfo.y = -1;
        moveState = MoveState.FADEOUT;
        deathbed = p;
        _controlCenter.GetComponent<GameManager>().moveAnimation = true;
    }

}
