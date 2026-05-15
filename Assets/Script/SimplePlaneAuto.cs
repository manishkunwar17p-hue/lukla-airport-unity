using UnityEngine;

public class SimplePlaneAuto : MonoBehaviour
{
    public Transform airportPoint;
    public Transform flyPoint;

    public float runwaySpeed = 20f;
    public float flySpeed = 30f;

    public float takeoffDistance = 50f;
    public float returnTime = 10f;

    float timer;

    bool runway = true;
    bool flying = false;
    bool returning = false;

    Vector3 runwayStart;

    void Start()
    {
        timer = returnTime;

        // remember runway start position
        runwayStart = transform.position;
    }

    void Update()
    {
        if (airportPoint == null || flyPoint == null) return;

        // =========================
        // RUNWAY TAKEOFF
        // =========================
        if (runway)
        {
            transform.position += transform.forward * runwaySpeed * Time.deltaTime;

            float dist = Vector3.Distance(runwayStart, transform.position);

            // after enough runway distance -> fly
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
        // RETURNING
        // =========================
        else if (returning)
        {
            MoveTo(airportPoint.position, flySpeed);

            float dist = Vector3.Distance(transform.position, airportPoint.position);

            if (dist < 2f)
            {
                returning = false;
            }
        }
    }

    void MoveTo(Vector3 target, float moveSpeed)
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

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    void OnGUI()
    {
        string status =
            runway ? "Runway Takeoff" :
            flying ? "Flying" :
            returning ? "Returning" :
            "Parked";

        GUI.Label(new Rect(10, 10, 300, 25), "Status: " + status);
        GUI.Label(new Rect(10, 30, 300, 25), "Altitude: " + transform.position.y.ToString("F1"));
    }
}