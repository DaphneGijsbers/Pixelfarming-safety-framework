using UnityEngine;

namespace Safety
{
    public interface IRobotPort
    {
        void Apply(RobotCommand cmd);
    }
}

