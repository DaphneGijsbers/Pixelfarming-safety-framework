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
        
        var go = new GameObject("agent");
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var robot = go.AddComponent<Robot>();          
        var patrol = go.AddComponent<FieldPatrol>();   

        var safetyField = typeof(FieldPatrol).GetField("safetyRobot",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (safetyField != null) safetyField.SetValue(patrol, robot);

        yield return null;

        robot.Apply(new RobotCommand { decision = Decision.Stop });

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var speed = rb.linearVelocity.magnitude;
        Assert.Less(speed, 1e-3f, "Rigidbody velocity should be ~0 after Stop");

        var start = go.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var dist = Vector3.Distance(start, go.transform.position);

        Assert.Less(dist, 1e-2f, $"Object moved {dist:F5}m while stopped");
    }
}
