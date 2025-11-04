// Assets/Tests/PlayMode/FieldPatrol_Gate_PlayMode_Tests.cs
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Safety;

public class FieldPatrol_Gate_PlayMode_Tests
{
    [UnityTest]
    public IEnumerator FieldPatrol_DoesNotMove_When_Robot_IsStopped()
    {
        // Arrange: robot object met alle benodigde componenten
        var go = new GameObject("agent");
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var robot = go.AddComponent<Robot>();          // bevat de safety-gate
        var patrol = go.AddComponent<FieldPatrol>();   // jouw motion controller

        // (Belangrijk) zorg dat FieldPatrol de gate vindt
        // Als je FieldPatrol een serialized veld 'safetyRobot' gaf:
        var safetyField = typeof(FieldPatrol).GetField("safetyRobot",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (safetyField != null) safetyField.SetValue(patrol, robot);

        // Laat Unity eerst Awake/Start afronden
        yield return null;

        // Act: latch STOP via de IRobotPort (Robot)
        robot.Apply(new RobotCommand { decision = Decision.Stop });

        // Wacht een paar physics ticks zodat Robot.FixedUpdate de stop kan afdwingen
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Assert: geen beweging (positie én snelheid nagenoeg 0)
        var speed = rb.linearVelocity.magnitude;
        Assert.Less(speed, 1e-3f, "Rigidbody velocity should be ~0 after Stop");

        // Position check: neem een snapshot na de Stop en check daarna opnieuw
        var start = go.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var dist = Vector3.Distance(start, go.transform.position);

        // Gebruik een iets ruimere tolerantie door floating point en MovePosition interpolatie
        Assert.Less(dist, 1e-2f, $"Object moved {dist:F5}m while stopped");
    }
}
