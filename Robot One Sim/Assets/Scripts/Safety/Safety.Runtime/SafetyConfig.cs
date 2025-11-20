using UnityEngine;

namespace Safety
{
    public sealed class SafetyConfig
    {
        public double ssm_stop_m = 2.0;
        public double ssm_slowdown_m = 5.0;

        public double obstacle_stop_m = 5.0;

        public double geofence_margin_m = 0.5;
        public double geofence_warn_m = 3.0;
        public double geofence_stop_m = 2.0;
        public double max_roll_deg = 20.0, max_pitch_deg = 20.0;
        public bool failstop_on_link_loss = true;
    }
}
