

namespace Safety
{
    // Simpele IRobotPort die bijhoudt wat er is toegepast
    public sealed class FakeRobotPort : IRobotPort
    {
        public RobotCommand Last;
        public int ApplyCount;
        public void Apply(RobotCommand cmd) { Last = cmd; ApplyCount++; }
    }

    // Kernel-dummy: jij bepaalt vooraf wat Evaluate teruggeeft
    public sealed class FakeKernel : ISafetyKernel
    {
        public SafetyResult Next;          // zet dit in je test
        public SafetyConfig UsedConfig;

        public bool Init(SafetyConfig cfg) { UsedConfig = cfg; return true; }
        public void Shutdown() { }
        public SafetyResult Evaluate(SensorSnapshot s) => Next ?? new SafetyResult();
    }
}
