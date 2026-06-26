extends CharacterBody2D
class_name MarioCharacter  # <─── ADD THIS LINE EXACTLY RIGHT HERE!

# 1. Define our structural states
enum Form { SMALL, SUPER }
enum Action { NORMAL, DUCKING }

var current_form = Form.SMALL
var current_action = Action.NORMAL

# 2. Reference your 4 unique collision nodes
@onready var col_small = $CollisionSmall
@onready var col_super = $CollisionSuper
@onready var col_small_duck = $CollisionSmallDuck
@onready var col_super_duck = $CollisionSuperDuck
@onready var sprite = $mario

# ==============================================================================
# DEBUG CONFIGURATION
# ==============================================================================
@export var debug: bool = true

# Onready reference to your canvas text node
@onready var debug_label: Label = $CanvasLayer/DebugLabel
@onready var animated_sprite = $mario

# 1. THE RESOLUTION SCALE MULTIPLIER
@export var resolution_scale: float = 1.0

const BASE_MAX_WALK_SPEED = 110.0
const BASE_MAX_RUN_SPEED  = 180.0
const BASE_ACCELERATION   = 84.0
const BASE_FRICTION       = 300.0
const BASE_SKID_FRICTION  = 133.0
const BASE_RUN_FRICTION   = 550.0  # <--- CRITICAL: Make sure this line is exactly here!

const BASE_JUMP_VELOCITY  = -315.0
const BASE_MIN_JUMP_VEL   = -100.0
const BASE_NORMAL_GRAVITY = 750.0
const BASE_FALL_GRAVITY   = 1050.0
const BASE_SPRINT_DECEL   = 450.0  # <--- CRITICAL: Make sure this line is here too!

# 3. RUNTIME ACTIVE VARIABLES (Calculated automatically)
var max_walk_speed: float
var max_run_speed: float
var acceleration: float
var friction: float
var run_friction: float
var skid_friction: float

var jump_velocity: float
var min_jump_velocity: float
var normal_gravity: float
var fall_gravity: float
var current_skid_dir: float = 0.0 # Tracks the direction you were moving when the skid started
var sprint_decel: float

func _ready() -> void:
	# Automatically register this character as a player in the physics world
	add_to_group("player")
	
	# Initialize your physics scaling factors when the level starts
	update_physics_scales()


func update_physics_scales() -> void:
	# Multiply pixel distances by your scale factor to preserve game feel
	max_walk_speed = BASE_MAX_WALK_SPEED * resolution_scale
	max_run_speed  = BASE_MAX_RUN_SPEED  * resolution_scale
	acceleration   = BASE_ACCELERATION   * resolution_scale
	sprint_decel   = BASE_SPRINT_DECEL   * resolution_scale
	friction       = BASE_FRICTION       * resolution_scale
	run_friction   = BASE_RUN_FRICTION   * resolution_scale
	skid_friction  = BASE_SKID_FRICTION  * resolution_scale

	jump_velocity     = BASE_JUMP_VELOCITY  * resolution_scale
	min_jump_velocity = BASE_MIN_JUMP_VEL   * resolution_scale
	normal_gravity    = BASE_NORMAL_GRAVITY * resolution_scale
	fall_gravity      = BASE_FALL_GRAVITY   * resolution_scale

func _physics_process(delta: float) -> void:
	# --- 1. SMOOTH MARIO GRAVITY ENGINE ---
	if not is_on_floor():
		if velocity.y > 0:
			# Falling down: Apply heavy gravity for snappy landing
			velocity.y += fall_gravity * delta
		else:
			# Rising up: Apply normal jump gravity
			velocity.y += normal_gravity * delta
			
		# FIXED VARIABLE JUMP: If player releases jump button while rising past min height, cap it!
		if Input.is_action_just_released("BTN_A") and velocity.y < min_jump_velocity:
			velocity.y = min_jump_velocity
	else:
		# Grounded Jump Trigger
		if Input.is_action_just_pressed("BTN_A"):
			velocity.y = jump_velocity

	# --- 2. SPEED TIER (Walk vs Run) ---
	var current_max_speed = max_walk_speed
	
	# FIXED: Checks if you are holding your sprint button to unlock max running speed
	if Input.is_action_pressed("BTN_B") and not (Input.is_action_pressed("DP_Down") and is_on_floor()):
		current_max_speed = max_run_speed

	# --- 3. HORIZONTAL MOVEMENT ---
	var direction := Input.get_axis("DP_Left", "DP_Right")
	var is_skidding := false

	if is_on_floor() and Input.is_action_pressed("DP_Down"):
		# Force Mario to slide to a halt if he ducks while moving
		velocity.x = move_toward(velocity.x, 0, friction * delta)
		current_skid_dir = 0.0
		current_action = Action.DUCKING
	else:
		current_action = Action.NORMAL
		if direction != 0:
			# SKID DETECTION: Shifting directions mid-run triggers extreme slide friction
			if velocity.x != 0 and sign(direction) != sign(velocity.x) and current_skid_dir == 0.0:
				if abs(velocity.x) > (max_walk_speed * 0.4):
					current_skid_dir = sign(velocity.x) # Lock original sliding vector

			# SKID CONDITION 2: Actively waiting for velocity to catch up to the new input direction
			if current_skid_dir != 0.0:
				velocity.x = move_toward(velocity.x, direction * current_max_speed, skid_friction * delta)
				is_skidding = true
				
				# BREAK OUT: If velocity has completely flipped or reached 0, stop skidding!
				if sign(velocity.x) == sign(direction) or velocity.x == 0:
					current_skid_dir = 0.0
					is_skidding = false
			else:
				# If you are holding the D-Pad but your speed is above current max speed (let go of run)
				if abs(velocity.x) > current_max_speed:
					velocity.x = move_toward(velocity.x, direction * current_max_speed, sprint_decel * delta)
				else:
					velocity.x = move_toward(velocity.x, direction * current_max_speed, acceleration * delta)
		else:
			# FIXED: If let go of all buttons, use a heavier friction if coming down from a sprint
			if abs(velocity.x) > max_walk_speed:
				velocity.x = move_toward(velocity.x, 0, run_friction * delta)
			else:
				velocity.x = move_toward(velocity.x, 0, friction * delta)
			current_skid_dir = 0.0

	# --- 4. UPDATE COLLISION SOLID SHAPES ---
	update_collision_states()

	# --- 5. ANIMATION ENGINE ---
	update_animation_engine(direction, is_skidding)

	# --- 6. EXECUTE ENGINE PHYSICS ---
	move_and_slide()

func update_collision_states() -> void:
	# Disable everything first to prevent multi-collisions
	col_small.disabled = true
	col_super.disabled = true
	col_small_duck.disabled = true
	col_super_duck.disabled = true
	
	# Enable the single shape matching Mario's current structural form
	if current_form == Form.SMALL:
		if current_action == Action.DUCKING:
			col_small_duck.disabled = false
		else:
			col_small.disabled = false
	elif current_form == Form.SUPER:
		if current_action == Action.DUCKING:
			col_super_duck.disabled = false
		else:
			col_super.disabled = false

func update_animation_engine(direction: float, is_skidding: bool) -> void:
	# Dynamically formats prefix string based on enum status
	var prefix = "small_" if current_form == Form.SMALL else "super_"
	
	if not is_on_floor():
		animated_sprite.speed_scale = 1.0
		animated_sprite.play(prefix + "jump")
	elif current_action == Action.DUCKING:
		animated_sprite.speed_scale = 1.0
		animated_sprite.play(prefix + "duck")
	elif is_skidding or (direction != 0 and velocity.x != 0 and sign(direction) != sign(velocity.x)):
		animated_sprite.play(prefix + "skid")
		var skid_percentage = abs(velocity.x) / max_run_speed
		animated_sprite.speed_scale = lerp(0.2, 1.4, skid_percentage)
		
		var face_dir = current_skid_dir if current_skid_dir != 0.0 else sign(velocity.x)
		animated_sprite.flip_h = (face_dir < 0)
	elif velocity.x != 0:
		if abs(velocity.x) > max_walk_speed + 10.0:
			animated_sprite.play(prefix + "run")
			var speed_percentage = abs(velocity.x) / max_run_speed
			animated_sprite.speed_scale = lerp(0.8, 1.6, speed_percentage)
		else:
			animated_sprite.play(prefix + "walk")
			var speed_percentage = abs(velocity.x) / max_walk_speed
			animated_sprite.speed_scale = lerp(0.4, 1.1, speed_percentage)
		
		animated_sprite.flip_h = (velocity.x < 0)
	else:
		animated_sprite.speed_scale = 1.0
		animated_sprite.play(prefix + "idle")

func _process(_delta: float) -> void:
	# Press the "T" key to toggle slow motion
	if Input.is_key_pressed(KEY_T):
		Engine.time_scale = 0.2  # Slow motion
	else:
		Engine.time_scale = 1.0  # Restore normal speed

	# --- QUICK RESTART TOOL ---
	if Input.is_action_just_pressed("Restart_Game"):
		get_tree().reload_current_scene()
		
	# --- DEBUG HUD ---
	if debug and debug_label:
		debug_label.show()
		var horizontal_speed = abs(velocity.x)
		var form_text = "SMALL" if current_form == Form.SMALL else "SUPER"
		
		debug_label.text = "- DEBUG -\n"
		debug_label.text += "Form: %s\n" % form_text
		debug_label.text += "Horizontal Speed: %0.2f px/s\n" % horizontal_speed
		debug_label.text += "Vertical Velocity: %0.2f\n" % velocity.y
		debug_label.text += "Is Grounded: %s\n" % ("YES" if is_on_floor() else "NO")
		debug_label.text += "Resolution Scale: %0.1fx" % resolution_scale
	elif debug_label:
		debug_label.hide()
