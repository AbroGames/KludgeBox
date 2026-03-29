using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace KludgeBox.Godot.Services;

public class AutoScalingService
{
    public record AutoScalingSettings(List<ScaleOption> ScaleOptions, float SmallestScaleFactor);
    public record ScaleOption(int MinimumWindowHeight, float ScaleFactor);

    public readonly AutoScalingSettings AutoScalingDefaultSettings = new(
        SmallestScaleFactor: 0.75f,
        ScaleOptions: [
            new(600, 0.875f),
            new(800, 1f),
            new(1200, 1.25f),
            new(1500, 1.5f)
        ]);

    private AutoScalingSettings _autoScalingSettings;
    private float _currentScale = 1;
    
    private SceneTree _sceneTree;
    
    [Logger] private ILogger _log;
    
    public AutoScalingService()
    {
        Di.Process(this);
    }
    
    public void Init(SceneTree sceneTree)
    {
        Init(sceneTree, AutoScalingDefaultSettings);
    }

    public void Init(SceneTree sceneTree, AutoScalingSettings autoScaleSettings)
    {
        _sceneTree = sceneTree;
        _sceneTree.ProcessFrame += Process;

        // Sorting value for fast search in the future
        _autoScalingSettings = autoScaleSettings with
        {
            ScaleOptions = autoScaleSettings.ScaleOptions.OrderByDescending(factor => factor.MinimumWindowHeight).ToList()
        };
    }

    private void Process()
    {
        var window = _sceneTree.Root;
        var size = window.Size;
        float newScale = GetScaleForWindowSize(window.Size.Y);

        if (!_currentScale.IsEqualApprox(newScale))
        {
            _log.Information("Adjusting scale for window size {sizeX}x{sizeY} to {newScale}", size.X, size.Y, newScale);
            _currentScale = newScale;
            _sceneTree.Root.ContentScaleFactor = newScale;
        }
    }
    
    private float GetScaleForWindowSize(int currentWindowHeight)
    {
        ScaleOption matchedScaleOption = _autoScalingSettings.ScaleOptions
            .FirstOrDefault(factor => factor.MinimumWindowHeight <= currentWindowHeight);

        return matchedScaleOption?.ScaleFactor ?? _autoScalingSettings.SmallestScaleFactor;
    }
}