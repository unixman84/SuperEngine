using Godot;
using System;

public partial class ItemBlock : StaticBody2D
{
    // ==============================================================================
    // 1. CONFIGURATION & STATE
    // ==============================================================================
    [Export] public PackedScene BlockContains;
    [Export] public int CoinCount = 1;

    public enum State { Unbumped, Bumped }
    private State _currentState = State.Unbumped;

    // ==============================================================================
    // 2. NODE REFERENCES
    // ==============================================================================
    private AnimatedSprite2D _sprites;
    private Area2D _bumpDetector;
    private float _initialY;
    private Node2D _lastPlayerToBump;


    // ==============================================================================
    // 3. INITIALIZATION
    // ==============================================================================
    public override void _Ready()
    {
        _initialY = GlobalPosition.Y;
        
        _sprites = GetNode<AnimatedSprite2D>("Sprites"); // Double check if your node is called "Sprites" or "AnimatedSprite2D"
        _bumpDetector = GetNode<Area2D>("BumpDetector");

        _bumpDetector.BodyEntered += OnBumpDetectorBodyEntered;
        _sprites.Play("block_full");
    }

    // ==============================================================================
    // 4. HIT DETECTION LOGIC
    // ==============================================================================
    private void OnBumpDetectorBodyEntered(Node2D body)
    {
        if (_currentState == State.Bumped)
        {
            return;
        }

        if (body.IsInGroup("player"))
        {
            // Remember exactly which player hit the block
            _lastPlayerToBump = body;
            
            TriggerBlockBounce();
            HandleItemReward();
        }
    }



    // ==============================================================================
    // 5. ITEM & BOUNCE PIPELINE
    // ==============================================================================
    private void TriggerBlockBounce()
    {
        Tween bounceTween = CreateTween();
        
        // 1. Pop UP quickly by 8 pixels
        bounceTween.TweenProperty(this, "global_position:y", _initialY - 8.0f, 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        
        // 2. Snap BACK DOWN to the original position
        bounceTween.TweenProperty(this, "global_position:y", _initialY, 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }

    private async void HandleItemReward()
    {
        CoinCount--;
        
        // If the block is now empty, lock it out and change the sprite frame
        if (CoinCount <= 0)
        {
            _currentState = State.Bumped;
            _sprites.Play("block_empty");
        }
        
        // Safely delay the spawning of the item by 0.2 seconds using C# tasks
        if (BlockContains != null)
        {
            await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
            SpawnItemInstance();
        }
    }

    private void SpawnItemInstance()
    {
        Node2D newItem = (Node2D)BlockContains.Instantiate();
        newItem.ZIndex = ZIndex - 1;
        
        GetParent().AddChild(newItem);
        newItem.GlobalPosition = new Vector2(GlobalPosition.X, _initialY);
        
        Tween spawnTween = CreateTween();
        float finalY = _initialY - 16.0f; // Pops up exactly 1 grid tile width
        
        spawnTween.TweenProperty(newItem, "global_position:y", finalY, 0.35f)
            .SetTrans(Tween.TransitionType.Linear);

        // NEW LINK: When the rise animation completes, unlock the mushroom's physics!
        spawnTween.Finished += () =>
        {
            // Verify the object is a SuperMushroom script type
            if (newItem is SuperMushroom mushroom && _lastPlayerToBump != null)
            {
                mushroom.LaunchInPlayerDirection(_lastPlayerToBump);
            }
        };
    }

}
    

