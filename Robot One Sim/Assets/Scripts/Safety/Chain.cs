using UnityEngine;
using System;

namespace Safety
{
    public interface ISafetyHandler {
        /// Return true als dit een finale beslissing is en de chain mag stoppen.
        bool Handle(SensorSnapshot s, SafetyResult r, SafetyKernel kernel);
    }

    // Voorbeeldhandlers (vul aan naar jouw HARA)
    public sealed class HumanHandler : ISafetyHandler {
        public bool Handle(SensorSnapshot s, SafetyResult r, SafetyKernel k) {
            var c = k.Config;
            if (s.estop_pressed) { r.decision = Decision.EStop; r.actions.Add("ToolDisable"); r.reasons.Add("EStop"); return true; }
            if (s.nearest_human_m < c.ssm_stop_m) { r.decision = Decision.Stop; r.actions.Add("ToolDisable"); r.reasons.Add("HumanProximity"); return true; }
            if (s.nearest_human_m < c.ssm_slowdown_m && r.decision == Decision.Allow) {
                r.decision = Decision.SlowDown; r.speed_limit_mps = 0.5f; r.reasons.Add("HumanNear");
            }
            return false;
        }
    }

    public sealed class GeofenceHandler : ISafetyHandler {
        public bool Handle(SensorSnapshot s, SafetyResult r, SafetyKernel k) {
            var c = k.Config;
            if (!s.inside_geofence || s.distance_to_boundary_m < c.geofence_margin_m) {
                r.decision = Decision.Stop; r.actions.Add("ToolDisable"); r.reasons.Add("Geofence");
                return true;
            }
            return false;
        }
    }

    public sealed class ObstacleHandler : ISafetyHandler {
        public bool Handle(SensorSnapshot s, SafetyResult r, SafetyKernel k) {
            var c = k.Config;
            if (s.nearest_obstacle_m < c.obstacle_stop_m) {
                r.decision = Decision.Stop;
                r.reasons.Add("ObstacleClose"); 
                Debug.Log($"Obstacle detected {s.nearest_obstacle_m:0.0} meters");
                return true;
            }
            return false;
        }
    }
}
