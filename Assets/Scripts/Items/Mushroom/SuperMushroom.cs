using Godot;
using System;

public partial class SuperMushroom : CharacterBody2D
{
    // ==============================================================================
    // 1. CONFIGURATION & COMPANION TRAIL VARIABLES
    // ==============================================================================
    [Export] public float Speed = 60.0f;
    [Export] public float Gravity = 500.0f;
    [Export] public float FollowDistance = 24.0f; // Target pixel offset behind the player

    private Vector2 _velocity = Vector2.Zero;
    private float _direction = 1.0f;
    private bool _isSpawning = true;
    private bool _isCollected = false;

    // Follow Companion Logic States
    private bool _isFollowingPlayer = false;
    private Node2D _targetPlayer = null;
    private bool _didPlayerJump = false;
    private float _playerJumpLaunchX = 0.0f;

    // ==============================================================================
    // 2. RESOURCE ENTRY & SETUPS
    // ==============================================================================
    public override void _Ready()
    {
        AddToGroup("items");

        if (GetNodeOrNull<Area2D>("CollectionArea") is Area2D collectionArea)
        {
            collectionArea.BodyEntered += OnCollectionAreaBodyEntered;
        }
    }

    // ==============================================================================
    // 2B. ENABLING THE COMPANION TRAILING STATE
    // ==============================================================================
    public void EnableCompanionFollow(Node2D player)
    {
        _isFollowingPlayer = true;
        _isSpawning = false;
        _targetPlayer = player;

        // SAFE PASSIVE LAYER: Disable layers so it is a ghost to Mario's active sweeps,
        // but keep mask active to let it land and bounce cleanly on the environment tiles.
        CollisionLayer = 0;
        CollisionMask = 1; 

        if (GetNodeOrNull<Area2D>("CollectionArea") is Area2D area)
        {
            area.Monitoring = false;
            area.Monitorable = false;
        }
    }

    // ==============================================================================
    // 3. PHYSICS PROCESSING LOOP
    // ==============================================================================
    public override void _PhysicsProcess(double delta)
    {
        if (_isCollected) return;

        // BRANCH A: Follow Trail Logic Engine
        if (_isFollowingPlayer && _targetPlayer != null && IsInstanceValid(_targetPlayer))
        {
            ExecuteCompanionTrail(delta);
            return;
        }

        // BRANCH B: Standard Autonomous Sliding Physics (For Question-Mark Blocks)
        if (_isSpawning) return;

        _velocity = Velocity;
        if (!IsOnFloor()) _velocity.Y += Gravity * (float)delta;
        _velocity.X = _direction * Speed;

        if (IsOnWall()) _direction *= -1.0f;

        Velocity = _velocity;
        MoveAndSlide();
    }

    // ==============================================================================
    // 4. THE POLISHED BREADCRUMB TRAILING ALGORITHM
    // ==============================================================================
    private void ExecuteCompanionTrail(double delta)
    {
        _velocity = Velocity;

        // Cast your player node to a CharacterBody2D to read active velocity packets safely
        if (_targetPlayer is CharacterBody2D playerPhysics)
        {
            // 1. BREADCRUMB LOGGING: Monitor Mario's jump frames live
            // If Mario leaves the ground going upward, log the exact X-coordinate where he launched!
            if (!playerPhysics.IsOnFloor() && playerPhysics.Velocity.Y < -50.0f && !_didPlayerJump)
            {
                _didPlayerJump = true;
                _playerJumpLaunchX = playerPhysics.GlobalPosition.X;
                GD.Print($">>> Logged player jump breadcrumb at X: {_playerJumpLaunchX} <<<");
            }

            // Reset the tracking trigger trigger loop once Mario lands safely back on a floor tile
            if (playerPhysics.IsOnFloor())
            {
                _didPlayerJump = false;
            }

            // 2. HORIZONTAL MOVEMENT CALCULATIONS
            float distanceToPlayer = GlobalPosition.X - playerPhysics.GlobalPosition.X;
            float absoluteDistance = Mathf.Abs(distanceToPlayer);

            var playerSprite = _targetPlayer.GetNodeOrNull<AnimatedSprite2D>("mario");
            float playerFacing = (playerSprite != null && playerSprite.FlipH) ? -1.0f : 1.0f;
            float targetXOffset = playerPhysics.GlobalPosition.X - (playerFacing * FollowDistance);

            if (absoluteDistance > FollowDistance)
            {
                float lerpSpeed = 0.15f; 
                _velocity.X = Mathf.Lerp(GlobalPosition.X, targetXOffset, lerpSpeed) - GlobalPosition.X;
                _velocity.X = Mathf.Clamp(_velocity.X / (float)delta, -Speed * 1.5f, Speed * 1.5f);
            }
            else
            {
                _velocity.X = Mathf.MoveToward(_velocity.X, 0, Speed * (float)delta);
            }

            // 3. RETRO DELAYED JUMP MECHANICS (THE COORDINATE TRIGGER)
            if (IsOnFloor())
            {
                // CRITICAL TRIGGER check: Did Mario jump, and has the companion finally arrived at that exact X spot?
                // Using an explicit 6-pixel proximity window to make sure it handles fast sprint entries perfectly!
                float currentDistanceToJumpSpot = Mathf.Abs(GlobalPosition.X - _playerJumpLaunchX);
                
                if (_didPlayerJump && currentDistanceToJumpSpot <= 6.0f)
                {
                    _velocity.Y = -280.0f; // Execute high platform jump exactly where Mario did!
                    _didPlayerJump = false; // Clear flag so it doesn't double-jump midair
                    GD.Print("Companion arrived at jump breadcrumb coordinate! Launching!");
                }
                else
                {
                    // Keep your perfect subtle idle floor shuffle exactly as you like it!
                    _velocity.Y = -70.0f; 
                }
            }
            else
            {
                _velocity.Y += Gravity * (float)delta; // Fall naturally down via gravity
            }
        }
        else
        {
            // Simple fallback default shuffle loop if target casting properties drop frame
            if (IsOnFloor()) _velocity.Y = -70.0f;
            else _velocity.Y += Gravity * (float)delta;
        }

        Velocity = _velocity;
        MoveAndSlide();
    }


    // ==============================================================================
    // 5. BLOCK COMPATIBILITY & GROUND COLLECTION
    // ==============================================================================
    public void LaunchInPlayerDirection(Node2D playerNode)
    {
        _isSpawning = false;
        var playerSprite = playerNode.GetNodeOrNull<AnimatedSprite2D>("mario");
        if (playerSprite != null) _direction = playerSprite.FlipH ? -1.0f : 1.0f;
    }

    private void OnCollectionAreaBodyEntered(Node2D body)
    {
        if (_isFollowingPlayer || _isCollected) return;

        if (body.IsInGroup("player") || body.Name.ToString().ToLower() == "player")
        {
            _isCollected = true;
            if (body.HasMethod("grow_mario")) 
            {
                body.Call("grow_mario");
            }
            QueueFree();
        }
    }
}
