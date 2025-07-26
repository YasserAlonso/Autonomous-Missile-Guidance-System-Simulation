using AutonomousMissileGuidance.Control;
using AutonomousMissileGuidance.Utils;
using System;
using System.Collections.Generic;

namespace AutonomousMissileGuidance.Models
{
    public class Missile
    {
        public MathHelpers.Vector2D Position { get; set; }
        public MathHelpers.Vector2D Velocity { get; set; }
        public double Heading { get; set; }
        public double Speed { get; set; }
        public double MaxTurnRate { get; set; }
        public double MaxSpeed { get; set; }
        public double Fuel { get; set; }
        public double MaxFuel { get; set; }
        public bool IsActive { get; set; }
        public string Id { get; set; }

        public IGuidanceAlgorithm GuidanceSystem { get; set; }
        public List<MathHelpers.Vector2D> Trail { get; private set; }
        
        private readonly Random _random = new Random();
        
        // PERFORMANCE: Reused objects to avoid GC pressure in update loop
        private static readonly MathHelpers.Vector2D _tempVector = new MathHelpers.Vector2D(0, 0);

        public Missile(MathHelpers.Vector2D startPosition, double heading, IGuidanceAlgorithm guidanceSystem)
        {
            Position = startPosition;
            Heading = heading;
            Speed = 200.0;
            MaxSpeed = 300.0;
            MaxTurnRate = Math.PI;
            Fuel = 100.0;
            MaxFuel = 100.0;
            IsActive = true;
            GuidanceSystem = guidanceSystem;
            Trail = new List<MathHelpers.Vector2D>();
            Id = Guid.NewGuid().ToString().Substring(0, 8);
            
            Velocity = MathHelpers.Vector2D.FromAngle(heading) * Speed;
        }

        public void Update(MathHelpers.Vector2D targetPosition, double deltaTime)
        {
            if (!IsActive || Fuel <= 0)
            {
                IsActive = false;
                return;
            }

            double controlSignal = GuidanceSystem.GetControlSignal(Position, targetPosition, Heading, deltaTime);
            
            double turnRate = MathHelpers.Clamp(controlSignal, -MaxTurnRate, MaxTurnRate);
            Heading += turnRate * deltaTime;
            Heading = MathHelpers.NormalizeAngle(Heading);

            Speed = Math.Min(Speed + 50.0 * deltaTime, MaxSpeed);
            
            Velocity = MathHelpers.Vector2D.FromAngle(Heading) * Speed;
            Position += Velocity * deltaTime;

            Fuel -= 2.0 * deltaTime;
            
            if (Trail.Count == 0 || MathHelpers.Distance(Position, Trail[Trail.Count - 1]) > 5.0)
            {
                Trail.Add(Position);
                if (Trail.Count > 100)
                    Trail.RemoveAt(0);
            }
        }

        public double DistanceToTarget(MathHelpers.Vector2D targetPosition)
        {
            return MathHelpers.Distance(Position, targetPosition);
        }

        public bool IsIntercept(MathHelpers.Vector2D targetPosition, double interceptRadius = 15.0)
        {
            return DistanceToTarget(targetPosition) <= interceptRadius;
        }

        public void Reset(MathHelpers.Vector2D startPosition, double heading)
        {
            Position = startPosition;
            Heading = heading;
            Speed = 200.0;
            Fuel = MaxFuel;
            IsActive = true;
            Trail.Clear();
            GuidanceSystem.Reset();
            Velocity = MathHelpers.Vector2D.FromAngle(heading) * Speed;
        }
    }
} 