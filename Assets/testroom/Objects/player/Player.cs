using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    /* ===============================
     *            FIELDS
     * ===============================*/
    readonly float grv = 0.01f;
    readonly float acc = 0.01f;
    readonly float frc = 0.02f;
    readonly float jmp = 0.15f;
    readonly float kickstep = 0.001f;
    readonly float kickmax = 50f;
    readonly float top = 0.15f;

    float ysp;
    float xsp;
    float kick;
    bool ball_kickable;
    bool do_kick;
    bool check_wall;
    bool check_floor;
    readonly float w = 0.5f;
    bool floor;
    float timer;

    bool input_x;
    bool jumping;


    [Serializable]
    class Ball {
        [HideInInspector] public float xsp;
        [HideInInspector] public float ysp;
        [HideInInspector] public readonly float r = 0.5f;
        [HideInInspector] public bool check_wall;
        [HideInInspector] public bool check_floor;
        public Transform transform = default;
        public Material default_material = default;
        public Material highlight_material = default;
        [HideInInspector] public bool floor;
        [HideInInspector] public bool wall;
    };
    [SerializeField] Ball ball = new Ball();

    [SerializeField] LayerMask blockLayer = default;

    [SerializeField] LineRenderer lineRenderer = default;

    /* ===============================
     *            EVENTS
     * ===============================*/
    
    private void Awake()
    {
        ball.transform = Instantiate(ball.transform, transform.position, Quaternion.identity);
        lineRenderer.positionCount = 0;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startWidth = lineRenderer.endWidth = 0.05f;
        lineRenderer.startColor = lineRenderer.endColor = new Color(0f, 1f, 0f, 1f);
    }

    private void Update()
    {
        input_x = false;
        if (Input.GetKey(KeyCode.A))
        {
            input_x = true;
            if (xsp > -top)
            {
                xsp -= acc;
                if (xsp < -top)
                    xsp = -top;
            }
        }
        if (Input.GetKey(KeyCode.D))
        {
            input_x = true;
            if (xsp < top)
            {
                xsp += acc;
                if (xsp > top)
                    xsp = top;
            }
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (!jumping)
            {
                ysp = jmp;
                jumping = true;
            }
        }
        if (Input.GetKey(KeyCode.K))
        {
            timer += Time.unscaledDeltaTime;
            if (timer > 0.4f)
            {
                if (timer > 0.8f)
                {
                    if (timer > 1.2f)
                    {
                        //transform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.magenta);
                        kick = 0.25f;
                    }
                    else
                    {
                        //transform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.blue);
                        kick = 0.20f;
                    }
                }
                else
                {
                    //transform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.green);
                    kick = 0.18f;
                }
            }
            else
            {
                //transform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.black);
                kick = top;
            }
        }
        if (Input.GetKeyUp(KeyCode.K))
        {
            do_kick = true;
            timer = 0f;
            //{ transform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red); }
        }
    }

    private void FixedUpdate()
    {
        PhysicsUpdate();
        PhysicsUpdate(ball);
    }

    /* ===============================
     *            METHODS
     * ===============================*/
    void PhysicsUpdate()
    {
        EnableCollision();

        floor = false;
        if (check_floor) { FloorCollision(); }
        if (check_wall) { WallCollision(); }

        // AIR
        if (!floor)
        {
            jumping = true;
            ysp -= grv;
        }

        // GROUND
        if (floor)
        {
            if (!input_x)
                xsp -= Mathf.Sign(xsp) * Mathf.Min(frc, Mathf.Abs(xsp));
        }

        // BALL
        float signtmp;
        if (Mathf.Abs(signtmp = transform.position.x + xsp - ball.transform.position.x) >= 3f)
        {
            if (xsp < ball.xsp)
            { xsp = ball.xsp; }
            transform.position = new Vector3(
                ball.transform.position.x + Mathf.Sign(signtmp) * 3f,
                transform.position.y,
                transform.position.z
            );
        }

        ball_kickable = false;
        ball.transform.gameObject.GetComponent<MeshRenderer>().material = ball.default_material;
        if ((Mathf.Abs(transform.position.x - ball.transform.position.x) < 1f)
        && (Mathf.Abs(transform.position.y - ball.transform.position.y) < 1f))
        {
            ball.transform.gameObject.GetComponent<MeshRenderer>().material = ball.highlight_material;
            ball_kickable = true;
            DrawKickLine();
            
        }
        else
        { lineRenderer.positionCount = 0; }

        if (do_kick)
        {
            if (ball_kickable)
            {
                ApplyKick(ball);
            }
            do_kick = false;
            kick = 0f;
        }

        transform.Translate(new Vector3(xsp, ysp, 0f));
    }
    void PhysicsUpdate(Ball ball)
    {
        EnableCollision(ball);
        ball.floor = false;
        ball.wall = false;
        if (ball.check_floor) { FloorCollision(ball); }
        if (ball.check_wall) { WallCollision(ball); }

        // AIR
        if (!ball.floor)
        { ball.ysp -= grv; }


        ball.transform.Translate(new Vector3(ball.xsp, ball.ysp, 0f));
    }

    void ApplyKick(Ball ball)
    {
        ball.xsp = -Mathf.Sign(transform.position.x + xsp - ball.transform.position.x) * kick;
        ball.ysp = kick + grv;
    }

    [SerializeField, Min(0)] int kickLineCount = 15;
    [SerializeField, Min(1)] int kickLineSpacing = 3;
    void DrawKickLine()
    {
        Ball test = new Ball();
        test.transform = Instantiate(ball.transform);
        ApplyKick(test);

        lineRenderer.positionCount = kickLineCount;
        lineRenderer.SetPosition(0, test.transform.position);
        for (int i = 1; i < kickLineCount; ++i)
        {
            for (int j = 0; j < kickLineSpacing; ++j)
            {
                PhysicsUpdate(test);
                if (ball.wall)
                {
                    lineRenderer.positionCount++;
                    lineRenderer.SetPosition(i, test.transform.position);
                    i++;
                }
            }
            lineRenderer.SetPosition(i, test.transform.position);
        }
        
        Destroy(test.transform.gameObject);
    }



    void EnableCollision()
    {
        check_floor = (ysp <= 0f);
        check_wall = (xsp != 0f);
    }
    void EnableCollision(Ball ball)
    {
        ball.check_floor = (ball.ysp <= 0f);
        ball.check_wall = (ball.xsp != 0f);
    }
    void FloorCollision()
    {
        if (Physics.Raycast(
            new Vector3(transform.position.x, transform.position.y + 0.01f, transform.position.z),
            Vector3.down,
            out RaycastHit hit,
            Mathf.Abs(ysp) + 0.01f,
            blockLayer
        ))
        {
            floor = true;
            jumping = false;
            ysp = 0f;
            transform.position = new Vector3(
                transform.position.x,
                hit.point.y,
                transform.position.z
            );
        }
    }
    void FloorCollision(Ball ball)
    {
        if (Physics.Raycast(
            ball.transform.position,
            Vector3.down,
            out RaycastHit hit,
            Mathf.Abs(ball.ysp) + ball.r,
            blockLayer
        ))
        {
            ball.floor = true;
            ball.xsp = 0f;
            ball.ysp = 0f;
            ball.transform.position = new Vector3(
                ball.transform.position.x,
                hit.point.y + ball.r - 0.01f,
                ball.transform.position.z
            );
        }
    }
    void WallCollision()
    {
        if (Physics.Raycast(
            transform.position,
            new Vector3(Mathf.Sign(xsp), 0f, 0f),
            out RaycastHit hit,
            w + Mathf.Abs(xsp),
            blockLayer
        ))
        {
            transform.position = new Vector3(
                hit.point.x - (Mathf.Sign(xsp) * w),
                transform.position.y,
                transform.position.z
            );
            xsp = 0f;
        }
    }
    void WallCollision(Ball ball)
    {
        if (Physics.Raycast(
            ball.transform.position,
            new Vector3(Mathf.Sign(ball.xsp), 0f, 0f),
            out RaycastHit hit,
            ball.r + Mathf.Abs(ball.xsp),
            blockLayer
        ))
        {
            ball.transform.position = new Vector3(
                hit.point.x - (Mathf.Sign(ball.xsp) * ball.r),
                ball.transform.position.y,
                ball.transform.position.z
            );
            ball.xsp = -ball.xsp * 0.5f;
            ball.wall = true;
        }
    }
}