using UnityEngine;

// IRobotPort.cs
namespace Safety
{
    public interface IRobotPort
    {
        void Apply(RobotCommand cmd);
    }
}

