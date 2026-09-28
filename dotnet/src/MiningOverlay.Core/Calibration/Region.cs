namespace MiningOverlay.Core.Calibration;

/// <summary>Capture region as fractions (0.0-1.0) of the target monitor's width/height.</summary>
public readonly record struct Region(double X, double Y, double W, double H);
