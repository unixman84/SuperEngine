using Godot;
using System;

public partial class SuperMushroom : CharacterBody2D
{
	// ==============================================================================
	// 1. CONFIGURATION & STATE
	// ==============================================================================
	[Export]
	public int ScoreReward = 1000;

	[Export]
	public float Speed = 60.0f;

	[Export]
	public float Gravity = 750.0f;

	private bool _isCollected = false;
	private Area2D _collectionArea;
	private Vector2 _velocity = Vector2.Zero;
	private float _direction = 1.0f; // 1.0 = Right, -1.0 = Left
	private bool _isSpawning = true;


	// ==============================================================================
	// 2. INITIALIZATION
	// ==============================================================================
	public override void _Ready()
	{
		_collectionArea = GetNode<Area2D>("CollectionArea");
		_collectionArea.BodyEntered += OnCollectionAreaBodyEntered;
	}

	// ==============================================================================
	// 3. PHYSICS & MOVEMENT LOOP
	// ==============================================================================
	public override void _PhysicsProcess(double delta)
	{
		// FIX: If the mushroom is still rising out of the block, freeze physics!
		if (_isSpawning)
		{
			Velocity = Vector2.Zero;
			return;
		}

		_velocity = Velocity;

		// Apply gravity if airborne
		if (!IsOnFloor())
		{
			_velocity.Y += Gravity * (float)delta;
		}

		// Apply horizontal speed
		_velocity.X = _direction * Speed;

		Velocity = _velocity;
		MoveAndSlide();

		// Flip direction if hitting a wall/pipe
		if (IsOnWall())
		{
			_direction *= -1.0f;
		}
	}


       // ==============================================================================
    // 4. LAUNCH LOGIC
    // ==============================================================================
    public void LaunchInPlayerDirection(Node2D playerNode)
    {
        // 1. Turn off the spawning freeze so physics engine activates!
        _isSpawning = false;

        // 2. Fetch Mario's animated sprite node to read his orientation
        var playerSprite = playerNode.GetNode<AnimatedSprite2D>("mario");
        
        if (playerSprite != null)
        {
            // If Mario is flipped, he faces Left (-1). Otherwise, he faces Right (1).
            _direction = playerSprite.FlipH ? -1.0f : 1.0f;
        }
    }



	// ==============================================================================
	// 5. TOUCH DETECTION LOGIC
	// ==============================================================================
	private void OnCollectionAreaBodyEntered(Node2D body)
	{
		if (_isCollected || !body.IsInGroup("player"))
		{
			return;
		}

		_isCollected = true;
		ExecuteCollectionSequence(body);
	}

	// ==============================================================================
	// 6. COLLECTION PIPELINE
	// ==============================================================================
	private void ExecuteCollectionSequence(Node2D playerNode)
	{
		GD.Print($"+{ScoreReward} Points!");

		if (playerNode.Get("current_form").AsInt32() == 0) // 0 is SMALL form
		{
			playerNode.Set("current_form", 1); // 1 is SUPER form
			GD.Print("Player grew to SUPER form!");
		}

		QueueFree();
	}
}
