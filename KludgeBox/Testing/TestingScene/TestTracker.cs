using Godot;

namespace KludgeBox.Testing.TestingScene;

public partial class TestTracker : HBoxContainer
{
    private static class Icons
    {
        public const string PassedIcon = "✅";
        public const string FailedIcon = "❌";
        public const string UnknownIcon = "❔";
        public const string SkippedIcon = "❕";

        public static string Map(TestResult result)
        {
            switch (result)
            {
                case TestResult.NotRan: return UnknownIcon;
                case TestResult.Skipped: return SkippedIcon;
                case TestResult.Unknown: return UnknownIcon;
                case TestResult.Passed: return PassedIcon;
                case TestResult.Failed: return FailedIcon;
                case TestResult.Errored: return FailedIcon;
                default: throw new Exception("Unknown result");
            }
        }
    }
    
    private static class StatusColors
    {
        
        private const float _colorsBrightness = 0.2f;
        private const float _colorsValue = 1f;
    
        public static readonly Color PassedColor = new Color(_colorsBrightness, _colorsValue, _colorsBrightness);
        public static readonly Color FailedColor = new Color(_colorsValue, _colorsBrightness, _colorsBrightness);
        public static readonly Color UnknownColor = new Color(_colorsValue, _colorsValue, _colorsBrightness);
        public static readonly Color SkippedColor = new Color(_colorsBrightness, _colorsValue, _colorsValue);
        
        private static Dictionary<TestResult, Color> _map = new()
        {
            { TestResult.NotRan, UnknownColor },
            { TestResult.Skipped, SkippedColor},
            { TestResult.Unknown, UnknownColor },
            { TestResult.Passed, PassedColor },
            { TestResult.Failed, FailedColor },
            { TestResult.Errored, FailedColor }
        };
        
        public static Color Map(TestResult result) => _map[result];
    }
    
    
    public TestContext Context { get; }

    private Label _testStatusIconLabel;
    private Label _testStatusTextLabel;
    private Label _testNameLabel;
    private Label _testMessageLabel;
    private PanelContainer _panel;

    private static int _checkInterval = 5;
    private int _ticksToCheck = _checkInterval;
    
    public TestTracker(TestContext testContext)
    {
        Context = testContext;
    }

    public override void _Ready()
    {
        BuildUi();
    }

    public override void _Process(double delta)
    {
        _ticksToCheck--;

        if (_ticksToCheck <= 0)
        {
            UpdateUi();
            _ticksToCheck = _checkInterval;
        }
    }

    private void UpdateUi()
    {
        var result = Context.Result;
        var color = StatusColors.Map(result);
        var icon = Icons.Map(result);
        
        _testNameLabel.Text = Context.Name;
        _testStatusIconLabel.Text = icon;
        
        _testStatusTextLabel.Text = $"{result}";
        _testStatusTextLabel.LabelSettings.FontColor = color;
        
        _testMessageLabel.Text = Context.ResultMessage;
        
        _panel.SelfModulate = color;
    }

    private void BuildUi()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        
        // add indentation
        for (int i = 0; i < Context.GetDepth(); i++)
        {
            var spacing = new Control();
            spacing.CustomMinimumSize = new Vector2(20, 0);
            AddChild(spacing);
        }

        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _panel = panel;
        AddChild(panel);
        var hBox = new HBoxContainer();
        hBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.AddChild(hBox);
        
        // add status icon
        _testStatusIconLabel = new Label();
        var iconSettings = new LabelSettings();
        iconSettings.FontSize = 24;
        _testStatusIconLabel.LabelSettings = iconSettings;
        hBox.AddChild(_testStatusIconLabel);
        
        // add test name and status text
        var vBox = new VBoxContainer();
        vBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hBox.AddChild(vBox);

        _testNameLabel = new Label();
        var nameSettings = new LabelSettings();
        nameSettings.FontSize = 14;
        nameSettings.OutlineColor = Colors.Black;
        nameSettings.OutlineSize = 2;
        _testNameLabel.LabelSettings = nameSettings;
        vBox.AddChild(_testNameLabel);
        
        _testStatusTextLabel = new Label();
        var statusSettings = new LabelSettings();
        statusSettings.FontSize = 14;
        statusSettings.OutlineColor = Colors.Black;
        statusSettings.OutlineSize = 2;
        _testStatusTextLabel.LabelSettings = statusSettings;
        vBox.AddChild(_testStatusTextLabel);
        
        // add message
        _testMessageLabel = new Label();
        var messageSettings = new LabelSettings();
        messageSettings.FontSize = 12;
        messageSettings.OutlineColor = Colors.Black;
        messageSettings.OutlineSize = 2;
        _testMessageLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _testMessageLabel.LabelSettings = messageSettings;
        _testMessageLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vBox.AddChild(_testMessageLabel);
    }
}