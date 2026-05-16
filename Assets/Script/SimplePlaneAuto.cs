using UnityEngine;

public class SimplePlaneAuto : MonoBehaviour
{
    public Transform airportPoint;   // final stop
    public Transform flyPoint;       // sky target
    public Transform landingPoint;   // start of runway

    public float runwaySpeed = 20f;
    public float flySpeed = 30f;

    public float takeoffDistance = 50f;
    public float returnTime = 10f;

    float timer;

    bool runway = true;
    bool flying = false;
    bool returning = false;
    bool landing = false;
    bool roll = false;

    Vector3 runwayStart;
    float currentSpeed;
    // =========================
    // START DELAY ADDED
    // =========================
    float startDelay = 4f;
    bool gameStarted = false;

    void Start()
    {
        timer = returnTime;
        runwayStart = transform.position;
        currentSpeed = runwaySpeed;
    }

    void Update()
    {
        if (airportPoint == null || flyPoint == null || landingPoint == null) return;

        // =========================
        // 4 SECOND START DELAY
        // =========================
        if (!gameStarted)
        {
            startDelay -= Time.deltaTime;

            if (startDelay <= 0f)
            {
                gameStarted = true;
            }

            return;
        }

        // =========================
        // TAKEOFF RUNWAY
        // =========================
        if (runway)
        {
            currentSpeed = runwaySpeed;

            transform.position += transform.forward * currentSpeed * Time.deltaTime;

            float dist = Vector3.Distance(runwayStart, transform.position);

            if (dist >= takeoffDistance)
            {
                runway = false;
                flying = true;
            }
        }

        // =========================
        // FLYING
        // =========================
        else if (flying)
        {
            MoveTo(flyPoint.position, flySpeed);

            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                flying = false;
                returning = true;
            }
        }

        // =========================
        // APPROACH LANDING
        // =========================
        else if (returning)
        {
            MoveTo(landingPoint.position, flySpeed * 0.6f);

            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, landingPoint.position.y, Time.deltaTime * 1.5f);
            transform.position = pos;

            float dist = Vector3.Distance(transform.position, landingPoint.position);

            if (dist < 10f)
            {
                returning = false;
                landing = true;
            }
        }

        // =========================
        // LANDING
        // =========================
        else if (landing)
        {
            MoveTo(landingPoint.position, runwaySpeed);

            Vector3 pos = transform.position;
            pos.y = landingPoint.position.y;
            transform.position = pos;

            float dist = Vector3.Distance(transform.position, landingPoint.position);

            if (dist < 2f)
            {
                landing = false;
                roll = true;
            }
        }

        // =========================
        // RUNWAY ROLL
        // =========================
        else if (roll)
        {
            MoveTo(airportPoint.position, runwaySpeed);

            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 0.8f);

            float dist = Vector3.Distance(transform.position, airportPoint.position);

            if (dist < 3f || currentSpeed < 0.5f)
            {
                currentSpeed = 0f;
                roll = false;
            }
        }
    }

    // =========================
    // MOVE CONTROL
    // =========================
    void MoveTo(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position).normalized;

        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                2f * Time.deltaTime
            );
        }

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    // =========================
    // UI
    // =========================
    void OnGUI()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.Box(new Rect(10, 10, 260, 140), "");

        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;

        if (runway) style.normal.textColor = Color.yellow;
        else if (flying) style.normal.textColor = Color.cyan;
        else if (returning) style.normal.textColor = Color.green;
        else if (landing) style.normal.textColor = Color.magenta;
        else style.normal.textColor = Color.white;

        string status =
            runway ? "Takeoff Runway" :
            flying ? "Flying" :
            returning ? "Approach Landing" :
            landing ? "Landing" :
            roll ? "Runway Roll" :
            "Stopped";

        GUI.Label(new Rect(20, 20, 230, 25), "Status: " + status, style);

        GUI.Label(new Rect(20, 55, 230, 25),
            "Altitude: " + transform.position.y.ToString("F1") + " m",
            style
        );

        GUI.Label(new Rect(20, 90, 230, 25),
            "Speed: " + currentSpeed.ToString("F1"),
            style
        );
    }
}