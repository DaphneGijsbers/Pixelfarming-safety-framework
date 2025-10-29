using UnityEngine;

using System.Collections.Generic;

namespace Safety
{
    public enum Decision { Allow, SlowDown, Stop, EStop }

    public sealed class SensorSnapshot {
        public double timestamp_s;
        public double x, y, heading_rad, speed_mps;
        public double nearest_human_m = 1e9, nearest_obstacle_m = 1e9;
        public bool inside_geofence = true;
        public double distance_to_boundary_m = 1e9;
        public double roll_deg, pitch_deg;
        public bool stability_warning;
        public bool tool_requested_active, tool_hw_interlock_ok = true;
        public bool link_ok = true, estop_pressed;
    }

    /*public sealed class SafetyConfig {
        public double ssm_stop_m = 2.0, ssm_slowdown_m = 5.0, obstacle_stop_m = 1.0, geofence_margin_m = 0.5;
        public double max_roll_deg = 20, max_pitch_deg = 20;
        public bool failstop_on_link_loss = true;
    }*/

    public sealed class SafetyResult {
        public Decision decision = Decision.Allow;
        public float speed_limit_mps = float.PositiveInfinity;
        public List<string> actions = new();
        public List<string> reasons = new();
    }

    // Command dat naar de robot teruggaat (kan uit mediator of chain komen)
    public sealed class RobotCommand {
        public Decision decision = Decision.Allow;
        public float speed_cap_mps = float.PositiveInfinity;
        public bool tool_enable = true;
        public bool estop = false;
        public List<string> reasons = new();
    }

    // Uitgaande poort naar robot/actuatoren (Unity motion controller of echte robot)
    //public interface IRobotPort {
       // void Apply(RobotCommand cmd);
    //}
}

