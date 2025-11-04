using NUnit.Framework;
using Safety;

public class Kernel_Obstacle_Tests
{
    [Test]
    public void Evaluate_Stop_When_Obstacle_Within_Threshold()
    {
        var cfg = new SafetyConfig { obstacle_stop_m = 5.0 };
        var kernel = new SafetyKernel();
        Assert.IsTrue(kernel.Init(cfg), "Kernel.Init should succeed and add handlers");

        var snap = new SensorSnapshot { nearest_obstacle_m = 4.0 };

        var res = kernel.Evaluate(snap);

        Assert.That(res.decision, Is.EqualTo(Decision.Stop));
        Assert.That(res.reasons, Does.Contain("ObstacleClose"));
    }
}
