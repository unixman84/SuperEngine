using Godot;
using System;

public partial class GrassBlock : StaticBody2D
{
    // ==============================================================================
    // 1. CONFIGURATION & COMPONENT HANDLES
    // ==============================================================================
    [Export] public PackedScene PullRewardScene; // Drag your SuperMushroom.tscn file here!
    
    private Area2D _pickDetector;
    private AudioStreamPlayer _pickSound;   
    
    private Node2D _activePlayer = null;
    private bool _isPlucked = false;

    // ==============================================================================
    // 2. RUNTIME RESOURCE ENTRY
    // ==============================================================================
    public override void _Ready()
    {
        _pickDetector = GetNode<Area2D>("PickDetector");
        _pickSound = GetNode<AudioStreamPlayer>("Pick");

        _pickDetector.BodyEntered += OnPickDetectorBodyEntered;
        _pickDetector.BodyExited += OnPickDetectorBodyExited;
    }

    // ==============================================================================
    // 3. INPUT MONITORS
    // ==============================================================================
    public override void _Process(double delta)
    {
        if (_activePlayer == null || _isPlucked) return;

        // Pluck input combination: Holding Down (DP_Down) and pressing Jump V (BTN_B)
        if (Input.IsActionPressed("DP_Down") && Input.IsActionJustPressed("BTN_B"))
        {
            ExecutePluckSequence();
        }
    }

    private void OnPickDetectorBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player") || body.Name.ToString().ToLower().Contains("player"))
        {
            _activePlayer = body;
            GD.Print(">>> Player entered harvest zone <<<");
        }
    }

    private void OnPickDetectorBodyExited(Node2D body)
    {
        if (body == _activePlayer)
        {
            _activePlayer = null;
        }
    }

    // ==============================================================================
    // 4. SHAPE-SHIFT CONDITIONAL HARVEST ENGINE
    // ==============================================================================
    private async void ExecutePluckSequence()
    {
        _isPlucked = true;
        _pickSound.Play();

        _activePlayer.SetPhysicsProcess(false);
        if (HasNode("Sprites")) GetNode<Node2D>("Sprites").Visible = false;

        await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);

        // 1. DYNAMIC NATIVE GDSCRIPT ANALYSIS: Read Mario's 'current_form' variable
        // 0 = Form.SMALL, 1 = Form.SUPER
        int currentFormIndex = 0;
        Variant rawForm = _activePlayer.Get("current_form");
        if (rawForm.VariantType != Variant.Type.Nil)
        {
            currentFormIndex = rawForm.AsInt32();
        }

        // 2. DISPATCH DESTINATION ROUTINE
        if (currentFormIndex == 0)
        {
            // BRANCH A: Mario is Small -> Instant consumption power-up!
            if (_activePlayer.HasMethod("grow_mario")) 
            {
                _activePlayer.Call("grow_mario");
            }
            
            GD.Print("Small Mario plucked grass: Instant Consume Transformation executed!");
        }
        else
        {
            // BRANCH B: Mario is Already Big -> Instantiate trailing companion queue!
            if (PullRewardScene != null)
            {
                Node2D reward = (Node2D)PullRewardScene.Instantiate();
                
                // Spawn it cleanly on the main level world plane, right over the grass spot coordinates
                GetParent().AddChild(reward);
                reward.GlobalPosition = GlobalPosition + new Vector2(0, -16f);

                if (reward is SuperMushroom mushroom)
                {
                    mushroom.EnableCompanionFollow(_activePlayer);
                }

                GD.Print("Big Mario plucked grass: Mushroom spawned into trailing companion queue!");
            }
        }

        _activePlayer.SetPhysicsProcess(true);
        QueueFree(); // Terminate spent grass tile instance cleanly
    }
}
