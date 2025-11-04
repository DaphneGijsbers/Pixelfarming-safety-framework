using NUnit.Framework;
using Safety;
using System.Collections.Generic;

public class Mediator_Stop_Tests
{
    [Test]
    public void Obstacle_Close_Triggers_Stop_And_Applies_To_Port()
    {
        var cfg = new SafetyConfig { obstacle_stop_m = 5.0, ssm_slowdown_m = 5.0 };
        var med = new Mediator(cfg);
        var port = new FakeRobotPort();
        var kernel = new FakeKernel {
            Next = new SafetyResult { decision = Decision.Stop, reasons = new List<string>{"ObstacleClose"} }
        };
        med.AttachRobotPort(port);
        med.AttachSafetyKernel(kernel);

        var snap = new SensorSnapshot {
            nearest_obstacle_m = 4.5, nearest_human_m = 10,
            inside_geofence = true, distance_to_boundary_m = 999, link_ok = true
        };

        var cmd = med.ProcessSensorData(snap);

        Assert.That(cmd.decision, Is.EqualTo(Decision.Stop));
        Assert.That(port.ApplyCount, Is.EqualTo(1));
        Assert.That(port.Last.reasons, Does.Contain("ObstacleClose"));
    }
}
