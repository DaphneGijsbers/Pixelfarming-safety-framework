using NUnit.Framework;
using Safety;

public class Geofence_Outside_Stop_Tests
{
    [Test]
    public void OutsideFence_Stops_Immediately()
    {
        var cfg = new SafetyConfig
        {
            geofence_warn_m = 3.0,
            geofence_stop_m = 0.5
        };
        var kernel = new SafetyKernel();
        Assert.IsTrue(kernel.Init(cfg), "Kernel.Init should add GeofenceHandler");

        var s = new SensorSnapshot
        {
            inside_geofence = false,   // buiten
            distance_to_boundary_m = 0
        };

        var r = kernel.Evaluate(s);

        Assert.That(r.decision, Is.EqualTo(Decision.Stop));
        Assert.That(r.reasons, Does.Contain("Geofence")); // of "GeofenceOutside" als je die label gebruikt
    }
}

