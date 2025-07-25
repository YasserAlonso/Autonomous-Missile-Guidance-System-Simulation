using AutonomousMissileGuidance.Utils;
using System;

namespace AutonomousMissileGuidance.Control
{
    public class PIDController : IGuidanceAlgorithm
    {
        public double Kp { get; set; }
        public double Ki { get; set; }
        public double Kd { get; set; }
        
        private double _integral;
        private double _lastError;
        private bool _firstUpdate = true;

        public string Name => "PID Controller";

        public PIDController(double kp = 1.0, double ki = 0.1, double kd = 0.05)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            Reset();
        }

        public double GetControlSignal(MathHelpers.Vector2D missilePosition, 
                                     MathHelpers.Vector2D targetPosition, 
                                     double missileHeading, 
                                     double deltaTime)
        {
            MathHelpers.Vector2D toTarget = targetPosition - missilePosition;
            double desiredHeading = toTarget.Angle;
            double error = MathHelpers.AngleDifference(missileHeading, desiredHeading);

            _integral += error * deltaTime;
            _integral = MathHelpers.Clamp(_integral, -10.0, 10.0);

            double derivative = 0;
            if (!_firstUpdate)
            {
                derivative = (error - _lastError) / deltaTime;
            }
            else
            {
                _firstUpdate = false;
            }

            double control = Kp * error + Ki * _integral + Kd * derivative;
            _lastError = error;

            return MathHelpers.Clamp(control, -Math.PI, Math.PI);
        }

        public void Reset()
        {
            _integral = 0;
            _lastError = 0;
            _firstUpdate = true;
        }
    }
} 