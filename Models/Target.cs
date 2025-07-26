using AutonomousMissileGuidance.Utils;
using System;
using System.Collections.Generic;

namespace AutonomousMissileGuidance.Models
{
    public enum TargetMovementType
    {
        Linear,
        Circular,
        Sinusoidal,
        Random,
        Stationary
    }

    public class Target
    {
        public MathHelpers.Vector2D Position { get; set; }
        public MathHelpers.Vector2D Velocity { get; set; }
        public double Speed { get; set; }
        public double Heading { get; set; }
        public TargetMovementType MovementType { get; set; }
        public List<MathHelpers.Vector2D> Trail { get; private set; }
        
        private MathHelpers.Vector2D _centerPosition;
        private double _circularRadius;
        private double _time;
        private double _sinusoidalAmplitude;
        private double _sinusoidalFrequency;
        private readonly Random _random = new Random();
        
        private double _lastUpdateTime = 0;
        private const double UPDATE_FREQUENCY = 0.1;
        private MathHelpers.Vector2D _cachedVelocity;
        private bool _disableTrails = false; 
        
        private static readonly MathHelpers.Vector2D _reusableVector = new MathHelpers.Vector2D(0, 0);

        public Target(MathHelpers.Vector2D startPosition, TargetMovementType movementType = TargetMovementType.Linear)
        {
            Position = startPosition;
            Speed = 60.0;  
            Heading = Math.PI / 4;
            MovementType = movementType;
            Trail = new List<MathHelpers.Vector2D>();
            _time = 0;
            
            _centerPosition = startPosition;
            _circularRadius = 100.0;
            _sinusoidalAmplitude = 50.0;
            _sinusoidalFrequency = 0.5;
            
            UpdateVelocity();
            _cachedVelocity = Velocity;
        }

        public void Update(double deltaTime)
        {
            _time += deltaTime;

           
            if (_time - _lastUpdateTime >= UPDATE_FREQUENCY)
            {
                switch (MovementType)
                {
                    case TargetMovementType.Linear:
                        UpdateLinearOptimized(deltaTime);
                        break;
                    case TargetMovementType.Circular:
                        UpdateCircularOptimized(deltaTime);
                        break;
                    case TargetMovementType.Sinusoidal:
                        UpdateSinusoidalOptimized(deltaTime);
                        break;
                    case TargetMovementType.Random:
                        UpdateRandomOptimized(deltaTime);
                        break;
                    case TargetMovementType.Stationary:
                        _cachedVelocity = new MathHelpers.Vector2D(0, 0);
                        break;
                }
                _lastUpdateTime = _time;
                Velocity = _cachedVelocity;
            }

            Position += _cachedVelocity * deltaTime;
            
            if (!_disableTrails && (Trail.Count == 0 || MathHelpers.Distance(Position, Trail[Trail.Count - 1]) > 15.0))
            {
                Trail.Add(Position);
                if (Trail.Count > 15)  
                    Trail.RemoveAt(0);
            }
        }

        private void UpdateLinearOptimized(double deltaTime)
        {
           
            if (_time > 3.0)  
            {
                Heading += (_random.NextDouble() - 0.5) * 0.5; 
                _cachedVelocity = MathHelpers.Vector2D.FromAngle(Heading) * Speed;
                _time = 0; 
            }
            else
            {
                _cachedVelocity = MathHelpers.Vector2D.FromAngle(Heading) * Speed;
            }
        }

        private void UpdateCircularOptimized(double deltaTime)
        {
            double angularVelocity = Speed / _circularRadius;
            Heading += angularVelocity * UPDATE_FREQUENCY;  // Smooth circular motion
            
            Position = _centerPosition + MathHelpers.Vector2D.FromAngle(Heading) * _circularRadius;
            _cachedVelocity = MathHelpers.Vector2D.FromAngle(Heading + Math.PI / 2) * Speed;
        }

        private void UpdateSinusoidalOptimized(double deltaTime)
        {
            double baseHeading = Math.PI / 4;
            double sinusoidalOffset = _sinusoidalAmplitude * Math.Sin(_sinusoidalFrequency * _time);
            
            double baseCos = Math.Cos(baseHeading);
            double baseSin = Math.Sin(baseHeading);
            double perpCos = Math.Cos(baseHeading + Math.PI / 2);
            double perpSin = Math.Sin(baseHeading + Math.PI / 2);
            
            _cachedVelocity = new MathHelpers.Vector2D(
                baseCos * Speed + perpCos * sinusoidalOffset,
                baseSin * Speed + perpSin * sinusoidalOffset
            );
        }

        private void UpdateRandomOptimized(double deltaTime)
        {
            if (_time > 1.5) 
            {
                Heading += (_random.NextDouble() - 0.5) * Math.PI;  // Full random turns
                Heading = MathHelpers.NormalizeAngle(Heading);
                _cachedVelocity = MathHelpers.Vector2D.FromAngle(Heading) * Speed;
                _time = 0;
            }
            else
            {
                
                _cachedVelocity = MathHelpers.Vector2D.FromAngle(Heading) * Speed;
            }
        }

        private void UpdateVelocity()
        {
            Velocity = MathHelpers.Vector2D.FromAngle(Heading) * Speed;
        }

        public void SetMovementType(TargetMovementType movementType)
        {
            MovementType = movementType;
            _centerPosition = Position;
            _time = 0;
            _lastUpdateTime = 0;
            UpdateVelocity();
            _cachedVelocity = Velocity;
        }
    }
} 