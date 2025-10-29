using UnityEngine;

namespace Safety
{
    public interface IMediator
    {
        void AttachRobotPort(IRobotPort port);
        void AttachSafetyKernel(ISafetyKernel kernel);

        /// Verwerkt sensordata en geeft een RobotCommand terug (bypass of via kernel).
        RobotCommand ProcessSensorData(SensorSnapshot s);
    }
}