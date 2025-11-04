using UnityEngine;

namespace Safety
{
    public interface IMediator
    {
        void AttachRobotPort(IRobotPort port);
        void AttachSafetyKernel(ISafetyKernel kernel);

        RobotCommand ProcessSensorData(SensorSnapshot s);
    }
}