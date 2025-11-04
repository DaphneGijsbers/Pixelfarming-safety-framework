using UnityEngine;

using System.Collections.Generic;

namespace Safety
{
    public sealed class SafetyKernel : ISafetyKernel
    {
        readonly List<ISafetyHandler> chain = new();
        public SafetyConfig Config { get; private set; }

        public bool Init(SafetyConfig cfg) {
            Config = cfg;
            chain.Clear();
            chain.Add(new HumanHandler());
            chain.Add(new GeofenceHandler());
            chain.Add(new ObstacleHandler());
            
            return true;
        }

        public void Shutdown() { chain.Clear(); }

        public SafetyResult Evaluate(SensorSnapshot snapshot) {
            var r = new SafetyResult();
            foreach (var h in chain)
                if (h.Handle(snapshot, r, this)) break;
            return r;
        }
    }
}
