class_name MushroomItem
extends CharacterBody2D

# ==============================================================================
# 1. CONFIGURATION & STATE
# ==============================================================================
@export var score_reward: int = 1000

# Track whether the mushroom has already been collected
var is_collected: bool = false
@onready var collection_area: Area2D = $CollectionArea

# ==============================================================================
# 2. INITIALIZATION
# ==============================================================================
func _on_collection_area_body_entered(body: Node2D) -> void:
	print("Something touched the mushroom: ", body.name) # ─── ADD THIS LINE!
	
	if is_collected or not body.is_in_group("player"):
		return

# ==============================================================================
# 3. TOUCH DETECTION LOGIC
# ==============================================================================
func _on_collection_area_body_entered(body: Node2D) -> void:
	# Ignore if already collected, or if the node touching us is NOT a player
	if is_collected or not body.is_in_group("player"):
		return
		
	is_collected = true
	execute_collection_sequence(body)

# ==============================================================================
# 4. COLLECTION PIPELINE
# ==============================================================================
func execute_collection_sequence(player: MarioCharacter) -> void:
	# 1. Award points
	print("+%d Points!" % score_reward)
	
	# 2. Tell Mario to transform into Super Form
	if mario.current_form == player.Form.SMALL:
		mario.current_form = player.Form.SUPER
		print("Mario grew to SUPER form!")
	
	# 3. Permanently remove the mushroom from the active game world
	queue_free()
