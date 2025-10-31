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

    public sealed class SafetyResult {
        public Decision decision = Decision.Allow;
        public float speed_limit_mps = float.PositiveInfinity;
        public List<string> actions = new();
        public List<string> reasons = new();
    }

    public sealed class RobotCommand {
        public Decision decision = Decision.Allow;
        public float speed_cap_mps = float.PositiveInfinity;
        public bool tool_enable = true;
        public bool estop = false;
        public List<string> reasons = new();
    }
}

