using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Safety;

public class Robot_Stop_PlayMode_Tests
{
    [UnityTest]
    public IEnumerator Robot_Stops_On_Stop_Command()
    {
        var go = new GameObject("robot");
        var rb = go.AddComponent<Rigidbody>(); rb.useGravity = false;
        var robot = go.AddComponent<Robot>();

        rb.linearVelocity = new Vector3(2,0,0); 

        robot.Apply(new RobotCommand { decision = Decision.Stop });

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.Less(rb.linearVelocity.magnitude, 1e-3f);
        Assert.IsTrue(robot.IsStopped);
        Assert.IsFalse(robot.IsEStopped);
    }
}
