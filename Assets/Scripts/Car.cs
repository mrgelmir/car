using System;
using System.Collections.Generic;
using UnityEngine;


public interface ISpringConfig
{
    float Strength { get; }
    float Damp { get; }
    float RestDistance { get; }
}

public interface IEngineConfig
{
    float GetTorque(float currentSpeed);
    float GetBrakeTorque(float input);
}

[Serializable]
public class EngineConfig : IEngineConfig
{
    // This is probably because of car weight?
    private const float Multiplier = 1000f;

    [SerializeField] private float topSpeed = 50f;
    [SerializeField] private float engineTorque = 1f;
    [SerializeField] private float brakeTorque = 3f;

    [SerializeField] private AnimationCurve enginePowerCurve = AnimationCurve.Constant(0f, 1f, 1f);

    public float GetTorque(float currentSpeed)
    {
        float topSpeedUnits = topSpeed / 3.6f;

        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(currentSpeed) / topSpeedUnits);
        return enginePowerCurve.Evaluate(normalizedSpeed) * engineTorque * Multiplier;
    }

    public float GetBrakeTorque(float input) => brakeTorque * input;
}

public interface ITireConfig
{
    float Mass { get; }
    float Grip { get; }
    float GetGrip(float sidePercentage);
}

[Serializable]
public class TireConfig : ITireConfig
{
    [field: SerializeField]
    public float Mass { get; private set; } = 10f;

    [field: SerializeField]
    [field: Range(0f, 1f)]
    public float Grip { get; private set; } = .8f;

    [SerializeField] private AnimationCurve gripCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);


    public float GetGrip(float sidePercentage)
    {
        return gripCurve.Evaluate(Mathf.Clamp01(sidePercentage)) * Grip;
    }
}


public class Unpowered : IEngineConfig
{
    private readonly IEngineConfig child;

    public Unpowered(IEngineConfig child)
    {
        this.child = child;
    }

    public float GetTorque(float currentSpeed) => 0f;

    public float GetBrakeTorque(float input) => child.GetBrakeTorque(input);
}

public class Car : MonoBehaviour, ISpringConfig
{
    [SerializeField] private Rigidbody body;

    [SerializeField] private Transform wheelRightFront;
    [SerializeField] private Transform wheelLeftFront;
    [SerializeField] private Transform wheelRightBack;
    [SerializeField] private Transform wheelLeftBack;

    [Space]
    [SerializeField] private float rideHeight = 1f;
    [SerializeField] private float springForce = 10f;
    [SerializeField] private float springDamp = 1f;

    [Space]
    [SerializeField] private EngineConfig engineConfig;

    [Space]
    [SerializeField] private float maxSteerAngle = 60f;
    [SerializeField] private TireConfig tireConfig;


    private List<Wheel> wheels;

    public float Strength => springForce * 1000;
    public float Damp => springDamp;
    public float RestDistance => rideHeight;

    public float Speed = 0f;


    private void Start()
    {
        wheels = new List<Wheel>
        {
            new(wheelRightFront, tireConfig, this, new Unpowered(engineConfig), wheelRightFront.GetChild(0)),
            new(wheelLeftFront, tireConfig, this, new Unpowered(engineConfig), wheelLeftFront.GetChild(0)),
            new(wheelRightBack, tireConfig, this, (engineConfig), wheelRightBack.GetChild(0)),
            new(wheelLeftBack, tireConfig, this, (engineConfig), wheelLeftBack.GetChild(0)),
        };
    }

    private void Update()
    {
        float steerAngle = Input.GetAxis("Horizontal") * maxSteerAngle;

        wheelRightFront.transform.localEulerAngles = Vector3.up * steerAngle;
        wheelLeftFront.transform.localEulerAngles = Vector3.up * steerAngle;

        if (Input.GetKeyUp(KeyCode.Space))
        {
            Vector3 targetPosition = body.position + Vector3.up * 2f;
            Vector3 forward = body.transform.forward;
            forward.y = 0f;

            ResetPosition(targetPosition, forward.normalized);
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            ResetPosition(Vector3.up * 2f, Vector3.forward);
        }
    }


    public int debugForce = 20000;

    private void FixedUpdate()
    {
        foreach (Wheel spring in wheels)
        {
            spring.ApplyForce(body);
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            body.AddForceAtPosition(transform.right * debugForce, body.centerOfMass);
        }

        Speed = body.velocity.magnitude * 3.6f;
    }

    private void ResetPosition(Vector3 targetPosition, Vector3 forward)
    {
        body.position = targetPosition;
        body.rotation = Quaternion.LookRotation(forward, Vector3.up);


        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        if (wheelRightFront)
        {
            DrawWheelGizmos(wheelRightFront);
        }

        if (wheelLeftFront)
        {
            DrawWheelGizmos(wheelLeftFront);
        }

        if (wheelRightBack)
        {
            DrawWheelGizmos(wheelRightBack);
        }

        if (wheelLeftBack)
        {
            DrawWheelGizmos(wheelLeftBack);
        }
    }

    private void DrawWheelGizmos(Transform wheel)
    {
        Vector3 wheelPos = wheel.position;

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(wheelPos, .1f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(wheelPos, wheelPos - wheel.up * rideHeight);
    }
}