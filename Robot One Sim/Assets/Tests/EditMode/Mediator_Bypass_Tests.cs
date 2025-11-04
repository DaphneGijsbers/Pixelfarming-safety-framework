using NUnit.Framework;
using Safety;


public class Mediator_Bypass_Tests
{
    Mediator med; FakeRobotPort port; FakeKernel kernel; SafetyConfig cfg;

    [SetUp]
    public void Setup()
    {
        cfg = new SafetyConfig { obstacle_stop_m = 5.0, ssm_slowdown_m = 5.0 };
        med = new Mediator(cfg);
        port = new FakeRobotPort();
        kernel = new FakeKernel();
        med.AttachRobotPort(port);
        med.AttachSafetyKernel(kernel);
    }

    [Test]
    public void TrivialSafe_Gives_Allow_And_Applies()
    {
        var snap = new SensorSnapshot {
            nearest_obstacle_m = 10, nearest_human_m = 10,
            inside_geofence = true, distance_to_boundary_m = 999,
            link_ok = true, stability_warning = false
        };

        var cmd = med.ProcessSensorData(snap);

        Assert.That(cmd.decision, Is.EqualTo(Decision.Allow));
        Assert.That(port.ApplyCount, Is.EqualTo(1));
        Assert.That(port.Last.decision, Is.EqualTo(Decision.Allow));
    }
}
