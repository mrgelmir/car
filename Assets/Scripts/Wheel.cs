using Unity.VisualScripting;
using UnityEngine;

internal class Wheel
{
    private readonly Transform root;
    private readonly ITireConfig tire;
    private readonly ISpringConfig spring;
    private readonly IEngineConfig engine;
    private readonly Transform visual;

    private Vector3 worldVelocity = Vector3.zero;
    private WheelVisual wheel;
    private Vector3 rootPosition;

    public Wheel(Transform root, ITireConfig tire, ISpringConfig spring, IEngineConfig engine, Transform visual)
    {
        this.root = root;
        this.tire = tire;
        this.spring = spring;
        this.engine = engine;
        this.visual = visual;

        rootPosition = root.position;
        wheel = root.GetComponentInChildren<WheelVisual>();
    }

    public void ApplyForce(Rigidbody body)
    {
        rootPosition = root.position;
        Ray wheelRay = new(rootPosition, -root.up);
        bool contact = Physics.Raycast(wheelRay, out RaycastHit hit, spring.RestDistance);
        worldVelocity = body.GetPointVelocity(rootPosition);

        wheel.SetData(worldVelocity.magnitude, contact);

        if (!contact)
        {
            visual.localPosition = Vector3.down * spring.RestDistance;
            return;
        }


        visual.localPosition = Vector3.down * hit.distance;

        float liftForce = GetLiftForce(hit);
        float torque = GetAcceleration(body);
        float steer = GetSteerForce();

        Vector3 up = root.up * liftForce;
        Vector3 forward = root.forward * torque;
        Vector3 right = root.right * steer;


        // Debug.DrawLine(root.position, rootPosition + worldVelocity, Color.grey);
        // Debug.DrawLine(rootPosition, rootPosition + up / 1000f, Color.green);
        Debug.DrawLine(rootPosition, rootPosition + right / 1000f, Color.red);
        Debug.DrawLine(rootPosition, rootPosition + forward / 1000f, Color.blue);


        Vector3 finalForce = up + forward + right;

        body.AddForceAtPosition(finalForce, rootPosition);
    }

    private float GetLiftForce(RaycastHit hit)
    {
        float offset = spring.RestDistance - hit.distance;
        float velocity = Vector3.Dot(root.up, worldVelocity);

        float force = (offset * spring.Strength) - (velocity * spring.Damp);

        return force;
    }

    private float GetAcceleration(Rigidbody body)
    {
        float accelerationInput = Input.GetAxis("Vertical");

        float forwardVelocity = GetForwardVelocity(worldVelocity);

        // Acceleration
        if (accelerationInput > float.Epsilon)
        {
            // TODO: Engine only starts providing based on total body velocity, not forward velocity
            // it should evaluate its torque based on current output, not on body velocity

            float torque = engine.GetTorque(forwardVelocity) * accelerationInput;
            // Debug.DrawLine(root.position, root.position + root.forward * torque / 100f, Color.blue);

            return torque;
        }

        // Breaking
        if (body.velocity.magnitude > 0f && accelerationInput < -float.Epsilon)
        {
            // Negate the speed we have in the forward direction
            float desiredVelocity = -forwardVelocity;
            float maxBrakeForce = desiredVelocity / Time.fixedDeltaTime;
            float brakeTorque = maxBrakeForce * engine.GetBrakeTorque(Mathf.Abs(accelerationInput));

            // Debug.DrawLine(root.position, root.position + root.forward * speed, Color.green);
            // Debug.DrawLine(root.position, root.position + root.forward * desiredVelocity, Color.red);

            return brakeTorque;
        }

        // TODO: reverse

        {
            // keep rolling in the tire direction when not accelerating by applying (part of) the sideways force forwards 
            float steeringVelocity = GetSidewaysVelocity(worldVelocity);
            float sidePercentage = GetSidePercentage(steeringVelocity);
            float gripRemainder = 1f; // - tire.GetGrip(sidePercentage);
            float transferredForce = Mathf.Abs(steeringVelocity) * gripRemainder;

            float desiredAcceleration = transferredForce / Time.fixedDeltaTime;
            // Debug.DrawLine(root.position, root.position + root.forward * desiredAcceleration / 100f, Color.magenta);

            return desiredAcceleration;
        }

        return 0f;
    }

    private float GetSteerForce()
    {
        Vector3 steeringDir = root.right;

        float steeringVelocity = GetSidewaysVelocity(worldVelocity);
        float sidePercentage = GetSidePercentage(steeringVelocity);


        // TODO: If world velocity is lower: less drift and more grip
        // How to evaluate that curve? velocity? force? Use wheel mass for this probably
        float desiredChange = -steeringVelocity * tire.GetGrip(sidePercentage);

        float desiredAcceleration = desiredChange / Time.fixedDeltaTime;
        Debug.DrawLine(root.position, root.position + steeringDir * desiredAcceleration / 100f, Color.cyan);


        return tire.Mass * desiredAcceleration;
    }

    private float GetForwardVelocity(Vector3 velocity) => Vector3.Dot(root.forward, velocity);
    private float GetSidewaysVelocity(Vector3 velocity) => Vector3.Dot(root.right, velocity);

    private float GetSidePercentage(float sideVelocity)
    {
        float sidePercentage = Mathf.Abs(sideVelocity / worldVelocity.magnitude);
        if (float.IsNaN(sidePercentage))
        {
            sidePercentage = 0f;
        }

        return sidePercentage;
    }
}