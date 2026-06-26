class_name FireFlower
extends CharacterBody2D

# ==============================================================================
# 1. MOVEMENT CONFIGURATION
# ==============================================================================
const SPEED = 60.0
var direction: float = 1.0 # 1.0 = Moves Right, -1.0 = Moves Left

# Pull gravity directly from your Godot global project settings
var gravity = ProjectSettings.get_setting("physics/2d/default_gravity")
var is_active: bool = false

# ==============================================================================
# 2. PHYSICS PROCESSING LOOP
# ==============================================================================
func _physics_process(delta):
	# Freeze physics calculations completely until the item block wakes it up
	if not is_active:
		return

	# 1. Apply natural falling gravity when airborne
	if not is_on_floor():
		velocity.y += gravity * delta

	# 2. Execute physics movement and calculate wall collisions
	move_and_slide()
