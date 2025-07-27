using AutonomousMissileGuidance.Models;
using AutonomousMissileGuidance.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutonomousMissileGuidance.Telemetry
{
    public class TelemetryData
    {
        public double Time { get; set; }
        public string MissileId { get; set; }
        public MathHelpers.Vector2D MissilePosition { get; set; }
        public MathHelpers.Vector2D TargetPosition { get; set; }
        public double Distance { get; set; }
        public double Heading { get; set; }
        public double Speed { get; set; }
        public double Fuel { get; set; }
        public double ControlSignal { get; set; }
        public string GuidanceMode { get; set; }
    }

    public class InterceptData
    {
        public double Time { get; set; }
        public string MissileId { get; set; }
        public MathHelpers.Vector2D InterceptPosition { get; set; }
        public double Accuracy { get; set; }
        public double FuelUsed { get; set; }
        public string GuidanceMode { get; set; }
    }

    public class Logger
    {
        public List<TelemetryData> TelemetryLog { get; private set; }
        public List<InterceptData> InterceptLog { get; private set; }
        public List<string> EventLog { get; private set; }

        public Logger()
        {
            TelemetryLog = new List<TelemetryData>();
            InterceptLog = new List<InterceptData>();
            EventLog = new List<string>();
        }

        public void LogMissileData(Missile missile, MathHelpers.Vector2D targetPosition, double currentTime)
        {
            var data = new TelemetryData
            {
                Time = currentTime,
                MissileId = missile.Id,
                MissilePosition = missile.Position,
                TargetPosition = targetPosition,
                Distance = MathHelpers.Distance(missile.Position, targetPosition),
                Heading = missile.Heading,
                Speed = missile.Speed,
                Fuel = missile.Fuel,
                GuidanceMode = missile.GuidanceSystem.Name
            };

            TelemetryLog.Add(data);
            
            if (TelemetryLog.Count > 10000)
            {
                TelemetryLog.RemoveRange(0, 5000);
            }
        }

        public void LogIntercept(Missile missile, Target target, double currentTime)
        {
            var interceptData = new InterceptData
            {
                Time = currentTime,
                MissileId = missile.Id,
                InterceptPosition = missile.Position,
                Accuracy = MathHelpers.Distance(missile.Position, target.Position),
                FuelUsed = missile.MaxFuel - missile.Fuel,
                GuidanceMode = missile.GuidanceSystem.Name
            };

            InterceptLog.Add(interceptData);
            
            string message = $"[{currentTime:F2}s] Missile {missile.Id} intercepted target at distance {interceptData.Accuracy:F1}m using {interceptData.GuidanceMode}";
            EventLog.Add(message);
            
            if (EventLog.Count > 100)
            {
                EventLog.RemoveAt(0);
            }
        }

        public void LogNoFlyZoneViolation(Missile missile, AutonomousMissileGuidance.Simulation.NoFlyZone zone, double currentTime)
        {
            string message = $"[{currentTime:F2}s] Missile {missile.Id} entered no-fly zone at ({zone.Center.X:F0}, {zone.Center.Y:F0})";
            EventLog.Add(message);
            
            if (EventLog.Count > 100)
            {
                EventLog.RemoveAt(0);
            }
        }

        public TelemetryData GetLatestMissileData(string missileId)
        {
            return TelemetryLog.Where(t => t.MissileId == missileId).LastOrDefault();
        }

        public List<TelemetryData> GetMissileHistory(string missileId, int maxCount = 100)
        {
            return TelemetryLog
                .Where(t => t.MissileId == missileId)
                .TakeLast(maxCount)
                .ToList();
        }

        public double GetAverageAccuracy()
        {
            return InterceptLog.Any() ? InterceptLog.Average(i => i.Accuracy) : 0;
        }

        public double GetAverageFuelUsage()
        {
            return InterceptLog.Any() ? InterceptLog.Average(i => i.FuelUsed) : 0;
        }

        public double GetAverageInterceptTime()
        {
            return InterceptLog.Any() ? InterceptLog.Average(i => i.Time) : 0;
        }

        public void Clear()
        {
            TelemetryLog.Clear();
            InterceptLog.Clear();
            EventLog.Clear();
        }

        public Dictionary<string, int> GetGuidanceModeStats()
        {
            return InterceptLog
                .GroupBy(i => i.GuidanceMode)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
} 