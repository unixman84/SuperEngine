class_name ItemBlock
extends StaticBody2D

# ==============================================================================
# 1. CONFIGURATION & STATE
# ==============================================================================
@export var block_contains: PackedScene  # Drag your Mushroom, Coin, etc. here!
@export var coin_count: int = 1          # Set to higher for multi-coin blocks

enum State { UNBUMPED, BUMPED }
var current_state: int = State.UNBUMPED

# ==============================================================================
# 2. NODE REFERENCES
# ==============================================================================
@onready var sprites = $Sprites  
@onready var bump_detector = $BumpDetector

# Keep track of where the block started in the world
var initial_y: float

# ==============================================================================
# 3. INITIALIZATION
# ==============================================================================
func _ready() -> void:
	# Store the original Y position so we always return to the right spot
	initial_y = global_position.y
	
	# Connect the collision detector signal
	bump_detector.body_entered.connect(_on_bump_detector_body_entered)
	
	# Start with your question mark loop running
	sprites.play("block_full")

# ==============================================================================
# 4. HIT DETECTION LOGIC
# ==============================================================================
func _on_bump_detector_body_entered(body: Node2D) -> void:
	if current_state == State.BUMPED:
		return
		
	if body is MarioCharacter:
		# Trigger the code-driven physical bounce
		trigger_block_bounce()
		
		# Give Mario his reward
func handle_item_reward() -> void:
	coin_count -= 1
	
	# Lock the block to empty immediately
	if coin_count <= 0:
		current_state = State.BUMPED
		sprites.play("block_empty")
		
	# Safely launch the item spawn
	if block_contains:
		spawn_item_instance()


# ==============================================================================
# 5. ITEM & BOUNCE PIPELINE (Code-Driven Movement with Delay)
# ==============================================================================
func trigger_block_bounce() -> void:
	var bounce_tween = create_tween()
	
	# 1. Pop UP by 8 pixels quickly (0.08 seconds)
	bounce_tween.tween_property(self, "global_position:y", initial_y - 8.0, 0.08).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	
	# 2. Snap BACK DOWN to the original position quickly (0.08 seconds)
	bounce_tween.tween_property(self, "global_position:y", initial_y, 0.08).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)

func handle_item_reward() -> void:
	coin_count -= 1
	
	# FIX: Immediately lock out further hits BEFORE the await timer delays execution
	if coin_count <= 0:
		current_state = State.BUMPED
		sprites.play("block_empty")
		
	# Delay the spawning of the item by 0.2 seconds safely
	if block_contains:
		await get_tree().create_timer(0.2).timeout
		spawn_item_instance()

func spawn_item_instance() -> void:
	var new_item = block_contains.instantiate()
	new_item.z_index = z_index - 1
	get_parent().add_child(new_item)
	
	# Anchor its start position to the initial_y (original spot)
	new_item.global_position = Vector2(global_position.x, initial_y)
	
	# Smoothly slide the item upward out of the block (0.35 seconds)
	var spawn_tween = create_tween()
	var final_y = initial_y - 16.0  # Pops up exactly 1 tile width
	spawn_tween.tween_property(new_item, "global_position:y", final_y, 0.35).set_trans(Tween.TRANS_LINEAR)
