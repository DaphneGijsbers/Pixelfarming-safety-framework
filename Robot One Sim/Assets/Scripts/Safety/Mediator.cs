using UnityEngine;


namespace Safety
{
    public sealed class Mediator : IMediator
    {
        IRobotPort robotPort;
        ISafetyKernel kernel;
        SafetyConfig cfg;

        public Mediator(SafetyConfig cfg) { this.cfg = cfg; }

        public void AttachRobotPort(IRobotPort port) => robotPort = port;
        public void AttachSafetyKernel(ISafetyKernel kernel)
        {
            this.kernel = kernel;
            this.kernel.Init(cfg);
        }

        bool logChain = true; // aan/uit schakelaar voor debug

        static string DebugSafetyResult(SafetyResult r)
        {
            return $"[SafetyChain] decision={r.decision} speed_limit={r.speed_limit_mps:0.00} " +
                   $"actions=[{string.Join(",", r.actions)}] reasons=[{string.Join(",", r.reasons)}]";
        }

        static string DebugRobotCommand(RobotCommand c)
        {
            return $"[Mediator→Robot] cmd={c.decision} cap={c.speed_cap_mps:0.00} " +
                   $"estop={c.estop} reasons=[{string.Join(",", c.reasons)}]";
        }


        public RobotCommand ProcessSensorData(SensorSnapshot s)
        {
            // --- 1) Bypass regels ---
            bool trivialSafe =
                !s.estop_pressed &&
                s.nearest_human_m > cfg.ssm_slowdown_m &&
                s.nearest_obstacle_m > cfg.obstacle_stop_m &&
                s.inside_geofence &&
                s.distance_to_boundary_m > cfg.geofence_margin_m &&
                s.link_ok &&
                !s.stability_warning;

            if (trivialSafe)
            {
                var cmd = new RobotCommand
                {
                    decision = Decision.Allow,
                    speed_cap_mps = float.PositiveInfinity,
                    tool_enable = true
                };
                robotPort?.Apply(cmd);
                return cmd;
            }

            // --- 2) Anders: via SafetyKernel (Chain) ---
            var safety = kernel.Evaluate(s);

            var routed = new RobotCommand
            {
                decision = safety.decision,
                speed_cap_mps = (safety.decision == Decision.SlowDown) ? safety.speed_limit_mps : (safety.decision == Decision.Allow ? float.PositiveInfinity : 0f),
                tool_enable = !safety.actions.Contains("ToolDisable"),
                estop = safety.decision == Decision.EStop,
                reasons = safety.reasons
            };

            robotPort?.Apply(routed);
            return routed;
        }
    }
}
