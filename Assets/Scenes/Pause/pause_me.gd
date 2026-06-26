extends CanvasLayer

func _ready() -> void:
	# Hide the pause screen immediately when the game starts
	visible = false

func _process(_delta: float) -> void:
	# Toggle pause state when BTN_Start is pressed
	if Input.is_action_just_pressed("BTN_Start"):
		toggle_pause()

func toggle_pause() -> void:
	var new_pause_state = not get_tree().paused
	get_tree().paused = new_pause_state
	visible = new_pause_state
