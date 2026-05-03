using UnityEngine;

/// <summary>
/// AutoPlaneFlight.cs  —  Fixed & Clean Version
/// 
/// EXACT LOOP:
///   1. TAXI     — runs on ground, builds speed
///   2. TAKEOFF  — nose up, climbs to sky
///   3. FLY      — circles in the sky
///   4. DESCEND  — nose down, glides toward runway
///   5. LAND     — touches down smoothly
///   6. GROUND RUN — rolls down the runway after landing
///   7. HOLD     — sits still for 10 seconds
///   8. Repeats from Step 1 FOREVER
///
/// SETUP:
///   1. Attach this script to your plane GameObject
///   2. Add Rigidbody: Mass=1, Drag=0.3, AngularDrag=2, UseGravity=FALSE
///   3. Press Play — 100% automatic!
/// </summary>
public class AutoPlaneFlight : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════

    [Header("── Ground Speed ──")]
    public float taxiSpeed          = 18f;
    public float landingRollSpeed   = 14f;
    public float taxiAccel          = 6f;
    public float brakeDecel         = 8f;

    [Header("── Flight Speed ──")]
    public float takeoffSpeed       = 28f;
    public float cruiseSpeed        = 40f;
    public float descentSpeed       = 24f;

    [Header("── Altitude ──")]
    public float cruiseAltitude     = 70f;
    public float climbRate          = 4f;
    public float descentRate        = 3.5f;

    [Header("── Sky Loop ──")]
    public float loopRadius         = 100f;
    public int   circlesBeforeLand  = 2;

    [Header("── Timing ──")]
    public float holdTime           = 10f;   // seconds on ground before next flight

    [Header("── Rotation ──")]
    public float turnSpeed          = 2.2f;
    public float bankAngle          = 28f;
    public float noseUpPitch        = 16f;
    public float noseDownPitch      = 8f;

    [Header("── Effects (Optional) ──")]
    public AudioSource engineAudio;
    public Transform   propeller;
    public float       propellerRPM = 1800f;

    // ═══════════════════════════════════════════════════════
    //  PHASES
    // ═══════════════════════════════════════════════════════
    private enum Phase
    {
        Taxi,
        Takeoff,
        FlyCircle,
        Descend,
        Land,
        GroundRun,
        Hold
    }

    // ═══════════════════════════════════════════════════════
    //  PRIVATE STATE
    // ═══════════════════════════════════════════════════════
    private Phase   phase         = Phase.Taxi;
    private float   groundY;
    private float   speed         = 0f;
    private float   holdTimer     = 0f;

    // circle flight
    private Vector3 circleCenter;
    private float   circleAngleDeg = 0f;
    private int     circleCount    = 0;

    // spawn point — plane always returns here
    private Vector3 spawnPos;
    private float   spawnYaw;

    private Rigidbody rb;

    // ═══════════════════════════════════════════════════════
    //  START
    // ═══════════════════════════════════════════════════════
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity    = false;
        rb.linearDamping        = 0.4f;
        rb.angularDamping    = 3f;
        rb.constraints   = RigidbodyConstraints.None;

        groundY   = transform.position.y;
        spawnPos  = transform.position;
        spawnYaw  = transform.eulerAngles.y;

        if (engineAudio != null) { engineAudio.loop = true; engineAudio.Play(); }

        EnterPhase(Phase.Taxi);
    }

    // ═══════════════════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════════════════
    void Update()
    {
        SpinPropeller();
        PitchAudio();

        switch (phase)
        {
            case Phase.Taxi:      DoTaxi();      break;
            case Phase.Takeoff:   DoTakeoff();   break;
            case Phase.FlyCircle: DoFlyCircle(); break;
            case Phase.Descend:   DoDescend();   break;
            case Phase.Land:      DoLand();      break;
            case Phase.GroundRun: DoGroundRun(); break;
            case Phase.Hold:      DoHold();      break;
        }
    }

    // ═══════════════════════════════════════════════════════
    //  PHASE 1 — TAXI
    //  Plane rolls forward on the runway, nose level, builds speed
    // ═══════════════════════════════════════════════════════
    void DoTaxi()
    {
        speed = Mathf.MoveTowards(speed, taxiSpeed, taxiAccel * Time.deltaTime);

        SnapToGround();
        LevelPlane(6f);
        MoveForward(speed);

        if (speed >= taxiSpeed - 0.5f)
        {
            // Compute circle center far ahead so plane has room to climb
            circleCenter    = transform.position
                            + transform.forward * (loopRadius + 40f);
            circleCenter.y  = groundY + cruiseAltitude;
            circleAngleDeg  = 180f;
            circleCount     = 0;
            EnterPhase(Phase.Takeoff);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  PHASE 2 — TAKEOFF
    //  Nose pitches up, plane climbs straight until cruise altitude
    // ═══════════════════════════════════════════════════════
    void DoTakeoff()
    {
        speed = Mathf.MoveTowards(speed, takeoffSpeed, 5f * Time.deltaTime);

        // Pitch nose up
        Quaternion wantRot = Quaternion.Euler(-noseUpPitch,
                                               transform.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation,
                                               wantRot, turnSpeed * Time.deltaTime);

        // Climb
        float newY = Mathf.MoveTowards(transform.position.y,
                                        groundY + cruiseAltitude,
                                        climbRate * Time.deltaTime);
        SetY(newY);
        MoveForward(speed);

        if (transform.position.y >= groundY + cruiseAltitude - 1f)
        {
            // Re-anchor circle center now we are in position
            circleCenter   = transform.position + transform.forward * loopRadius;
            circleCenter.y = groundY + cruiseAltitude;
            circleAngleDeg = 180f;
            circleCount    = 0;
            EnterPhase(Phase.FlyCircle);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  PHASE 3 — FLY CIRCLE
    //  Orbits a fixed sky point, banks on turns
    // ═══════════════════════════════════════════════════════
    void DoFlyCircle()
    {
        speed = Mathf.MoveTowards(speed, cruiseSpeed, 4f * Time.deltaTime);

        // Advance angle around circle
        float degPerSec    = (speed / loopRadius) * Mathf.Rad2Deg;
        circleAngleDeg    += degPerSec * Time.deltaTime;

        if (circleAngleDeg >= 360f)
        {
            circleAngleDeg -= 360f;
            circleCount++;
            Debug.Log($"[AutoPlane] Circle {circleCount}/{circlesBeforeLand} complete.");
        }

        // Target position on circle rim at cruise altitude
        float   rad       = circleAngleDeg * Mathf.Deg2Rad;
        Vector3 targetPos = new Vector3(
            circleCenter.x + Mathf.Sin(rad) * loopRadius,
            groundY + cruiseAltitude,
            circleCenter.z + Mathf.Cos(rad) * loopRadius
        );

        // Tangent direction (direction of travel along circle)
        Vector3 toCenter = (circleCenter - transform.position);
        toCenter.y       = 0f;
        Vector3 tangent  = Vector3.Cross(toCenter.normalized, Vector3.up).normalized;

        // Bank calculation
        Vector3 flatFwd  = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        float   cross    = Vector3.Cross(flatFwd, tangent).y;
        float   bank     = -cross * bankAngle;

        Quaternion wantRot = Quaternion.LookRotation(tangent, Vector3.up);
        wantRot            = Quaternion.Euler(wantRot.eulerAngles.x,
                                               wantRot.eulerAngles.y,
                                               bank);
        transform.rotation = Quaternion.Slerp(transform.rotation,
                                               wantRot, turnSpeed * Time.deltaTime);

        // Glide to target position
        transform.position = Vector3.MoveTowards(transform.position,
                                                   targetPos,
                                                   speed * Time.deltaTime);

        if (circleCount >= circlesBeforeLand)
            EnterPhase(Phase.Descend);
    }

    // ═══════════════════════════════════════════════════════
    //  PHASE 4 — DESCEND
    //  Plane noses down and heads toward the runway
    // ═══════════════════════════════════════════════════════
    void DoDescend()
    {
        speed = Mathf.MoveTowards(speed, descentSpeed, 5f * Time.deltaTime);

        // Aim at a point just above the spawn
        Vector3 landPoint  = spawnPos + Vector3.up * 0.5f;
        Vector3 flatDir    = landPoint - transform.position;
        flatDir.y          = 0f;

        if (flatDir.magnitude > 1f)
        {
            Quaternion wantRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            wantRot            = Quaternion.Euler(noseDownPitch,
                                                   wantRot.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                   wantRot, turnSpeed * Time.deltaTime);
        }

        // Sink toward ground
        float newY = Mathf.MoveTowards(transform.position.y,
                                        groundY + 1.2f,
                                        descentRate * Time.deltaTime);
        SetY(newY);
        MoveForward(speed);

        if (transform.position.y <= groundY + 2f)
            EnterPhase(Phase.Land);
    }

    // ═══════════════════════════════════════════════════════
    //  PHASE 5 — LAND
    //  Plane flares, touches down, wheels hit the ground
    // ═══════════════════════════════════════════════════════
    void DoLand()
    {
        speed = Mathf.MoveTowards(speed, landingRollSpeed, 10f * Time.deltaTime);

        SnapToGround();
        LevelPlane(5f);          // level wings and pitch for touchdown
        MoveForward(speed);

        // Once level and on ground move to ground run
        float pitch = NormAngle(transform.eulerAngles.x);
        float roll  = NormAngle(transform.eulerAngles.z);
        if (Mathf.Abs(pitch) < 3f && Mathf.Abs(roll) < 3f)
            EnterPhase(Phase.GroundRun);
    }

    // ═══════════════════════════════════════════════════════
    //  PHASE 6 — GROUND RUN
    //  Rolls down the runway and slows to a stop
    // ═══════════════════════════════════════════════════════
    void DoGroundRun()
    {
        speed = Mathf.MoveTowards(speed, 0f, brakeDecel * Time.deltaTime);

        SnapToGround();
        LevelPlane(8f);
        MoveForward(speed);

        if (speed <= 0.05f)
        {
            speed     = 0f;
            holdTimer = 0f;
            rb.linearVelocity = Vector3.zero;
            EnterPhase(Phase.Hold);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  PHASE 7 — HOLD  (10 seconds still on the ground)
    //  Plane waits, then resets position and loops again
    // ═══════════════════════════════════════════════════════
    void DoHold()
    {
        rb.linearVelocity = Vector3.zero;
        holdTimer        += Time.deltaTime;

        if (holdTimer >= holdTime)
        {
            // Teleport back to spawn point facing original direction
            transform.position = spawnPos;
            transform.rotation = Quaternion.Euler(0f, spawnYaw, 0f);
            speed              = 0f;
            EnterPhase(Phase.Taxi);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════

    void EnterPhase(Phase next)
    {
        phase = next;
        Debug.Log($"[AutoPlane] ── Phase → {phase} ──");
    }

    // Move rb in facing direction
    void MoveForward(float spd)
    {
        rb.linearVelocity = transform.forward * spd;
    }

    // Lock Y to ground
    void SnapToGround()
    {
        transform.position = new Vector3(transform.position.x,
                                          groundY,
                                          transform.position.z);
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    // Set just the Y
    void SetY(float y)
    {
        transform.position = new Vector3(transform.position.x,
                                          y,
                                          transform.position.z);
    }

    // Smoothly level pitch and roll to zero
    void LevelPlane(float speed)
    {
        Quaternion flat = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, flat,
                                               speed * Time.deltaTime);
    }

    // Convert 0-360 → -180 to +180
    float NormAngle(float a) => a > 180f ? a - 360f : a;

    void SpinPropeller()
    {
        if (propeller == null) return;
        float s = Mathf.Lerp(150f, propellerRPM, speed / cruiseSpeed);
        propeller.Rotate(Vector3.forward, s * Time.deltaTime);
    }

    void PitchAudio()
    {
        if (engineAudio == null) return;
        engineAudio.pitch = Mathf.Lerp(0.35f, 1.9f, speed / cruiseSpeed);
    }

    // ═══════════════════════════════════════════════════════
    //  HUD
    // ═══════════════════════════════════════════════════════
    void OnGUI()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(10, 10, 295, 178), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle b = new GUIStyle { fontSize = 17, fontStyle = FontStyle.Bold };
        GUIStyle s = new GUIStyle { fontSize = 14, fontStyle = FontStyle.Normal };

        b.normal.textColor =
            phase == Phase.FlyCircle  ? Color.cyan    :
            phase == Phase.Takeoff    ? Color.yellow  :
            phase == Phase.Descend    ? new Color(1f,0.5f,0f) :
            phase == Phase.Hold       ? Color.gray    :
                                         Color.white;

        float alt = transform.position.y - groundY;

        GUI.Label(new Rect(18, 14,  270, 26), $"  PHASE   :  {phase}", b);

        b.normal.textColor = Color.white;
        GUI.Label(new Rect(18, 40,  270, 26), $"  SPEED   :  {speed:F1}", b);
        GUI.Label(new Rect(18, 66,  270, 26), $"  ALT     :  {alt:F1} m",   b);

        s.normal.textColor = new Color(0.5f, 1f, 0.5f);

        if (phase == Phase.FlyCircle)
            GUI.Label(new Rect(18, 100, 270, 22),
                $"  Circles : {circleCount} / {circlesBeforeLand}", s);
        else if (phase == Phase.Hold)
        {
            float remaining = holdTime - holdTimer;
            GUI.Label(new Rect(18, 100, 270, 22),
                $"  Takeoff in: {remaining:F1}s", s);
        }
        else
            GUI.Label(new Rect(18, 100, 270, 22), "  Auto flight active", s);

        s.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        GUI.Label(new Rect(18, 124, 270, 22), "  Taxi → Fly → Land → Run", s);
        GUI.Label(new Rect(18, 144, 270, 22), "  → Hold 10s → Repeat", s);
        GUI.Label(new Rect(18, 162, 270, 22), "  No input needed", s);
    }

    // ═══════════════════════════════════════════════════════
    //  SCENE GIZMOS
    // ═══════════════════════════════════════════════════════
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        int seg = 48;
        for (int i = 0; i < seg; i++)
        {
            float a1 = (i     / (float)seg) * Mathf.PI * 2f;
            float a2 = ((i+1) / (float)seg) * Mathf.PI * 2f;
            Gizmos.DrawLine(
                circleCenter + new Vector3(Mathf.Sin(a1),0,Mathf.Cos(a1)) * loopRadius,
                circleCenter + new Vector3(Mathf.Sin(a2),0,Mathf.Cos(a2)) * loopRadius
            );
        }

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(spawnPos, 2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(spawnPos.x, groundY, spawnPos.z), 2f);
    }
}