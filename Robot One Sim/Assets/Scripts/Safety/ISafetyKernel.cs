using UnityEngine;

namespace Safety
{
    public interface ISafetyKernel
    {
        bool Init(SafetyConfig cfg);
        void Shutdown();
        SafetyResult Evaluate(SensorSnapshot snapshot); // enkelvoudige call: input -> result
    }
}
