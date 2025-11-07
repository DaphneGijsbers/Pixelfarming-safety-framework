using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Safety;

public class SimFeeder_Integration_Tests
{
    [UnityTest]
    public IEnumerator SimFeeder_To_Mediator_To_Robot_Stop()
    {
        var robotGo = new GameObject("robot");
        var rb = robotGo.AddComponent<Rigidbody>(); rb.useGravity = false;
        var robot = robotGo.AddComponent<Robot>();

        var feeder = robotGo.AddComponent<SimFeeder>();

        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.transform.position = robotGo.transform.position + robotGo.transform.forward * 3f;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.IsTrue(robot.IsStopped, "Robot should be latched stopped");
        Assert.Less(rb.linearVelocity.magnitude, 1e-3f);
    }
}
