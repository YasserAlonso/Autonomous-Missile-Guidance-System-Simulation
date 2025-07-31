using AutonomousMissileGuidance.Control;
using AutonomousMissileGuidance.Models;
using AutonomousMissileGuidance.Simulation;
using AutonomousMissileGuidance.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AutonomousMissileGuidance
{
    public partial class MainWindow : Window
    {
        private Simulator _simulator = null!;
        private DispatcherTimer _timer = null!;
        private bool _showTrails = true;
        private bool _addingNoFlyZone = false;
        private List<double> _distanceHistory = null!;
        private List<double> _velocityHistory = null!;
        private List<double> _timeHistory = null!;
        public MainWindow()
        {
            InitializeComponent();
            InitializeSimulation();
            SetupTimer();
            InitializeGraph();
            ConfigureWindow();
            
            Loaded += MainWindow_Loaded;
        }

        private void ConfigureWindow()
        {
            Width = 1200;
            Height = 800;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _simulator.AddTarget(new MathHelpers.Vector2D(600, 200), TargetMovementType.Circular);
            _simulator.AddMissile(new MathHelpers.Vector2D(100, 500), 
                                Math.PI / 4, new PIDController(1.0, 0.1, 0.05));
            
            _simulator.AddNoFlyZone(new MathHelpers.Vector2D(400, 300), 80);
            
            UpdateUI();
        }

        private void InitializeSimulation()
        {
            _simulator = new Simulator(800, 600);
            _simulator.OnIntercept += OnMissileIntercept;
            _simulator.OnSimulationStep += UpdateUI;
            
            _distanceHistory = new List<double>();
            _velocityHistory = new List<double>();
            _timeHistory = new List<double>();
        }

        private void SetupTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _timer.Tick += Timer_Tick;
        }

        private void InitializeGraph()
        {
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _simulator?.Update();
        }

        private void UpdateUI()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    DrawSimulation();
                    UpdateTelemetry();
                    UpdateEventLog();
                    UpdateGraph();
                    UpdateMissionTime();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI Update Error: {ex.Message}");
            }
        }

        private void DrawSimulation()
        {
            if (SimulationCanvas == null) return;
            
            SimulationCanvas.Children.Clear();
            
            if (ShowNoFlyZonesCheckBox?.IsChecked == true)
            {
                DrawNoFlyZones();
            }
            
            DrawTargets();
            DrawMissiles();
        }

        private void DrawNoFlyZones()
        {
            if (_simulator?.NoFlyZones == null || SimulationCanvas == null) return;

            foreach (var zone in _simulator.NoFlyZones)
            {
                var ellipse = new Ellipse
                {
                    Width = zone.Radius * 2,
                    Height = zone.Radius * 2,
                    Fill = FindResource("NoFlyZoneBrush") as Brush ?? Brushes.Red,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    Opacity = 0.3
                };
                
                Canvas.SetLeft(ellipse, zone.Center.X - zone.Radius);
                Canvas.SetTop(ellipse, zone.Center.Y - zone.Radius);
                SimulationCanvas.Children.Add(ellipse);
            }
        }

        private void DrawTargets()
        {
            if (_simulator?.Targets == null || SimulationCanvas == null) return;

            foreach (var target in _simulator.Targets)
            {
                if (_showTrails && target.Trail != null)
                {
                    DrawTrail(target.Trail, Brushes.Orange);
                }
                
                var targetShape = new Ellipse
                {
                    Width = 16,
                    Height = 16,
                    Fill = FindResource("TargetBrush") as Brush ?? Brushes.Red,
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                };
                
                Canvas.SetLeft(targetShape, target.Position.X - 8);
                Canvas.SetTop(targetShape, target.Position.Y - 8);
                SimulationCanvas.Children.Add(targetShape);
            }
        }

        private void DrawMissiles()
        {
            if (_simulator?.Missiles == null || SimulationCanvas == null) return;

            foreach (var missile in _simulator.Missiles)
            {
                if (!missile.IsActive) continue;
                
                if (_showTrails && missile.Trail != null)
                {
                    DrawTrail(missile.Trail, Brushes.LightGreen);
                }
                
                var missileShape = CreateMissileShape(missile);
                if (missileShape != null)
                {
                    SimulationCanvas.Children.Add(missileShape);
                }
                
                var fuelBar = CreateFuelBar(missile);
                if (fuelBar != null)
                {
                    SimulationCanvas.Children.Add(fuelBar);
                }

                CheckMissileIntercepts(missile);
            }
        }

        private void CheckMissileIntercepts(Missile missile)
        {
            if (_simulator?.Targets == null) return;

            foreach (var target in _simulator.Targets.ToList())
            {
                if (missile.IsIntercept(target.Position))
                {
                    CreateExplosionEffect(missile.Position);
                    missile.IsActive = false;
                    _simulator.Targets.Remove(target);
                    break;
                }
            }
        }

        private Polygon CreateMissileShape(Missile missile)
        {
            if (missile == null) return null;

            var triangle = new Polygon
            {
                Fill = FindResource("MissileBrush") as Brush ?? Brushes.Blue,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            
            double size = 12;
            var points = new PointCollection
            {
                new Point(0, -size * 1.5),
                new Point(-size, size),
                new Point(0, size/2),
                new Point(size, size)
            };
            
            triangle.Points = points;
            
            var transform = new TransformGroup();
            transform.Children.Add(new RotateTransform(missile.Heading * 180 / Math.PI + 90));
            transform.Children.Add(new TranslateTransform(missile.Position.X, missile.Position.Y));
            triangle.RenderTransform = transform;
            
            return triangle;
        }

        private Rectangle CreateFuelBar(Missile missile)
        {
            if (missile == null) return null;

            var fuelPercent = Math.Max(0, Math.Min(1, missile.Fuel / missile.MaxFuel));
            var fuelBar = new Rectangle
            {
                Width = 20 * fuelPercent,
                Height = 3,
                Fill = fuelPercent > 0.3 ? Brushes.Green : Brushes.Red
            };
            
            Canvas.SetLeft(fuelBar, missile.Position.X - 10);
            Canvas.SetTop(fuelBar, missile.Position.Y - 20);
            
            return fuelBar;
        }

        private void DrawTrail(List<MathHelpers.Vector2D> trail, Brush brush)
        {
            if (trail == null || trail.Count < 2 || SimulationCanvas == null) return;
            
            for (int i = 1; i < trail.Count; i++)
            {
                var line = new Line
                {
                    X1 = trail[i - 1].X,
                    Y1 = trail[i - 1].Y,
                    X2 = trail[i].X,
                    Y2 = trail[i].Y,
                    Stroke = brush,
                    StrokeThickness = 1,
                    Opacity = 0.6
                };
                SimulationCanvas.Children.Add(line);
            }
        }
        #endregion

        private void UpdateTelemetry()
        {
            if (_simulator == null) return;

            UpdateTextBlock(SimTimeText, $"Time: {_simulator.SimulationTime:F1}s");
            UpdateTextBlock(MissileCountText, $"Missiles: {_simulator.Missiles.Count(m => m.IsActive)}");
            UpdateTextBlock(TargetCountText, $"Targets: {_simulator.Targets.Count}");
            
            var activeMissile = _simulator.Missiles.FirstOrDefault(m => m.IsActive);
            if (activeMissile != null)
            {
                UpdateActiveMissileTelemetry(activeMissile);
            }
            else
            {
                ClearMissileTelemetry();
            }
        }

        private void UpdateActiveMissileTelemetry(Missile missile)
        {
            if (_simulator?.Targets == null) return;

            var target = _simulator.Targets.FirstOrDefault();
            if (target != null)
            {
                var distance = missile.DistanceToTarget(target.Position);
                UpdateTextBlock(DistanceText, $"Distance: {distance:F1}m");
                
                UpdateGraphHistory(distance, missile.Speed);
            }
            
            UpdateTextBlock(HeadingText, $"Heading: {missile.Heading * MathHelpers.RadiansToDegrees:F1}°");
            UpdateTextBlock(SpeedText, $"Speed: {missile.Speed:F1} m/s");
            UpdateTextBlock(FuelText, $"Fuel: {missile.Fuel:F1}%");
            UpdateTextBlock(GuidanceModeText, $"Guidance: {missile.GuidanceSystem?.Name ?? "Unknown"}");
        }

        private void ClearMissileTelemetry()
        {
            UpdateTextBlock(DistanceText, "Distance: --");
            UpdateTextBlock(HeadingText, "Heading: --");
            UpdateTextBlock(SpeedText, "Speed: --");
            UpdateTextBlock(FuelText, "Fuel: --");
            UpdateTextBlock(GuidanceModeText, "Guidance: --");
        }

        private void UpdateTextBlock(System.Windows.Controls.TextBlock textBlock, string text)
        {
            if (textBlock != null)
            {
                textBlock.Text = text;
            }
        }

        private void UpdateGraphHistory(double distance, double speed)
        {
            if (_distanceHistory == null || _velocityHistory == null || _timeHistory == null) return;
            if (_simulator == null) return;

            _distanceHistory.Add(distance);
            _velocityHistory.Add(speed);
            _timeHistory.Add(_simulator.SimulationTime);
            
            const int maxPoints = 100;
            if (_distanceHistory.Count > maxPoints)
            {
                _distanceHistory.RemoveAt(0);
                _velocityHistory.RemoveAt(0);
                _timeHistory.RemoveAt(0);
            }
        }

        private void UpdateGraph()
        {
            if (GraphCanvas == null || _distanceHistory == null || _distanceHistory.Count < 2) return;
            
            GraphCanvas.Children.Clear();
            DrawGraphAxes();
            
            var graphWidth = GraphCanvas.ActualWidth - 40;
            var graphHeight = GraphCanvas.ActualHeight - 40;
            
            if (graphWidth <= 0 || graphHeight <= 0) return;
            
            DrawCurve(_distanceHistory, Brushes.LightBlue, graphWidth, graphHeight, 500);
            DrawCurve(_velocityHistory, Brushes.Orange, graphWidth, graphHeight, 300);
        }

        private void DrawGraphAxes()
        {
            if (GraphCanvas == null || GraphCanvas.ActualWidth <= 0 || GraphCanvas.ActualHeight <= 0)
                return;
                
            var xAxis = new Line
            {
                X1 = 20, Y1 = GraphCanvas.ActualHeight - 20,
                X2 = GraphCanvas.ActualWidth - 20, Y2 = GraphCanvas.ActualHeight - 20,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(xAxis);
            
            var yAxis = new Line
            {
                X1 = 20, Y1 = 20,
                X2 = 20, Y2 = GraphCanvas.ActualHeight - 20,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(yAxis);
        }

        private void DrawCurve(List<double> data, Brush brush, double width, double height, double maxValue)
        {
            if (data == null || data.Count < 2 || GraphCanvas == null) return;
            
            var points = new PointCollection();
            for (int i = 0; i < data.Count; i++)
            {
                var x = 20 + (i / (double)(data.Count - 1)) * width;
                var y = height + 20 - (data[i] / maxValue) * height;
                points.Add(new Point(x, Math.Max(20, Math.Min(height + 20, y))));
            }
            
            var polyline = new Polyline
            {
                Points = points,
                Stroke = brush,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
            
            GraphCanvas.Children.Add(polyline);
        }

        private void UpdateMissionTime()
        {
            if (_simulator == null) return;

            var timeSpan = TimeSpan.FromSeconds(_simulator.SimulationTime);
            var timeString = $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds / 100:D1}";
            
            UpdateTextBlock(MissionTimeValue, timeString);
            UpdateTextBlock(MissionTimeValueRight, timeString);
        }

        private void UpdateEventLog()
        {
            if (EventLogText == null || _simulator?.Logger?.EventLog == null) return;

            var events = _simulator.Logger.EventLog.TakeLast(10).Reverse();
            EventLogText.Text = string.Join("\n", events);
        }
        private void OnMissileIntercept(Missile missile, Target target)
        {
            if (missile == null || target == null) return;

            Dispatcher.Invoke(() =>
            {
                CreateExplosionEffect(missile.Position);
            });
        }

        private void CreateExplosionEffect(MathHelpers.Vector2D position)
        {
            if (SimulationCanvas == null) return;

            var explosion = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = Brushes.Yellow,
                Opacity = 0.8
            };
            
            Canvas.SetLeft(explosion, position.X - 20);
            Canvas.SetTop(explosion, position.Y - 20);
            SimulationCanvas.Children.Add(explosion);
            
            var fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            fadeTimer.Tick += (s, e) =>
            {
                explosion.Opacity -= 0.1;
                if (explosion.Opacity <= 0)
                {
                    SimulationCanvas.Children.Remove(explosion);
                    fadeTimer.Stop();
                }
            };
            fadeTimer.Start();
        }
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _simulator?.Start();
            _timer?.Start();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _simulator?.Pause();
            _timer?.Stop();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _simulator?.Reset();
            _timer?.Stop();
            
            _distanceHistory?.Clear();
            _velocityHistory?.Clear();
            _timeHistory?.Clear();
            
            if (GraphCanvas != null)
            {
                GraphCanvas.Children.Clear();
                DrawGraphAxes();
            }
            
            UpdateUI();
        }

        private void LaunchMissileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_simulator == null) return;

            var guidanceSystem = CreateCurrentGuidanceSystem();
            var startPos = new MathHelpers.Vector2D(100 + _simulator.Missiles.Count * 50, 500);
            _simulator.AddMissile(startPos, Math.PI / 4, guidanceSystem);
        }

        private IGuidanceAlgorithm CreateCurrentGuidanceSystem()
        {
            if (GuidanceComboBox?.SelectedIndex == 0)
            {
                return new PIDController(
                    KpSlider?.Value ?? 1.0, 
                    KiSlider?.Value ?? 0.1, 
                    KdSlider?.Value ?? 0.05);
            }
            else
            {
                return new ProportionalNavigation(NavConstantSlider?.Value ?? 3.0);
            }
        }
        private void GuidanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PIDPanel == null || PNPanel == null) return;
            
            if (GuidanceComboBox?.SelectedIndex == 0)
            {
                PIDPanel.Visibility = Visibility.Visible;
                PNPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                PIDPanel.Visibility = Visibility.Collapsed;
                PNPanel.Visibility = Visibility.Visible;
            }
        }

        private void PIDSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AreControlsInitialized()) return;
            
            UpdateTextBlock(KpValue, KpSlider!.Value.ToString("F2"));
            UpdateTextBlock(KiValue, KiSlider!.Value.ToString("F2"));
            UpdateTextBlock(KdValue, KdSlider!.Value.ToString("F2"));
            
            UpdateActiveMissilePIDParameters();
        }

        private void PNSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (NavConstantValue == null || NavConstantSlider == null) return;
            
            NavConstantValue.Text = NavConstantSlider.Value.ToString("F2");
        }

        private void UpdateActiveMissilePIDParameters()
        {
            if (!AreControlsInitialized() || _simulator?.Missiles == null) return;
            
            foreach (var missile in _simulator.Missiles.Where(m => m.IsActive && m.GuidanceSystem is PIDController))
            {
                if (missile.GuidanceSystem is PIDController pid)
                {
                    pid.Kp = KpSlider!.Value;
                    pid.Ki = KiSlider!.Value;
                    pid.Kd = KdSlider!.Value;
                }
            }
        }

        private bool AreControlsInitialized()
        {
            return KpValue != null && KiValue != null && KdValue != null &&
                   KpSlider != null && KiSlider != null && KdSlider != null;
        }

        private void NoiseCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_simulator?.Sensor != null && NoiseEnabledCheckBox != null)
            {
                _simulator.Sensor.NoiseEnabled = NoiseEnabledCheckBox.IsChecked == true;
            }
        }

        private void NoiseSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (NoiseValue == null || NoiseSlider == null) return;
            
            NoiseValue.Text = NoiseSlider.Value.ToString("F1");
            if (_simulator?.Sensor != null)
            {
                _simulator.Sensor.NoiseLevel = NoiseSlider.Value;
            }
        }

        private void TargetMovementComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_simulator?.Targets == null || !_simulator.Targets.Any() || TargetMovementComboBox == null) 
                return;

            var movementType = (TargetMovementType)Math.Max(0, TargetMovementComboBox.SelectedIndex);
            _simulator.Targets.First().SetMovementType(movementType);
        }

        private void AddTargetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_simulator == null) return;

            var random = new Random();
            var pos = new MathHelpers.Vector2D(
                random.NextDouble() * 800,
                random.NextDouble() * 600
            );
            
            var movementType = (TargetMovementType)Math.Max(0, TargetMovementComboBox?.SelectedIndex ?? 0);
            _simulator.AddTarget(pos, movementType);
        }

        private void ShowTrails_Changed(object sender, RoutedEventArgs e)
        {
            if (ShowTrailsCheckBox != null)
            {
                _showTrails = ShowTrailsCheckBox.IsChecked == true;
            }
        }

        private void AddNoFlyZoneButton_Click(object sender, RoutedEventArgs e)
        {
            _addingNoFlyZone = true;
            Cursor = Cursors.Cross;
            MessageBox.Show("Click on the canvas to add a no-fly zone", "Add No-Fly Zone", 
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_addingNoFlyZone || SimulationCanvas == null || _simulator == null) return;

            var pos = e.GetPosition(SimulationCanvas);
            _simulator.AddNoFlyZone(new MathHelpers.Vector2D(pos.X, pos.Y), 60);
            _addingNoFlyZone = false;
            Cursor = Cursors.Arrow;
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_simulator == null) return;

            _simulator.Missiles.Clear();
            _simulator.Targets.Clear();
            _simulator.NoFlyZones.Clear();
            _simulator.Logger.Clear();
            UpdateUI();
        }
    }
} 