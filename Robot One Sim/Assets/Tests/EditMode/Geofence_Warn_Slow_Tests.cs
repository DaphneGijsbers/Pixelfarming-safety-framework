using NUnit.Framework;
using Safety;

public class Geofence_Warn_Slow_Tests
{
    [Test]
    public void InsideFence_But_WithinWarn_SlowsDown()
    {
        var cfg = new SafetyConfig
        {
            geofence_warn_m = 3.0, // warning band
            geofence_stop_m = 0.5  // harde stop aan de rand
        };
        var kernel = new SafetyKernel();
        Assert.IsTrue(kernel.Init(cfg));

        var s = new SensorSnapshot
        {
            inside_geofence = true,
            distance_to_boundary_m = 1.5f // binnen warn (≤3.0) maar > stop (0.5)
        };

        var r = kernel.Evaluate(s);

        Assert.That(r.decision, Is.EqualTo(Decision.SlowDown),
            "Binnen warn-band moet SlowDown gekozen worden.");
        Assert.Greater(r.speed_limit_mps, 0f,
            "SlowDown zou een positieve cap moeten zetten (bijv. 0.5 m/s).");
        // Als je een specifieke reden/tag gebruikt:
        // Assert.That(r.reasons, Does.Contain("GeofenceNear"));
    }
}
