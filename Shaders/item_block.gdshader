shader_type canvas_item;

// Exposed variables that appear in your inspector layout
uniform vec2 scroll_speed = vec2(0.5, 0.0); // X speed, Y speed
uniform float scale_multiplier = 1.0;

void fragment() {
    // Offset the texture coordinates over time based on our custom speed vector
    vec2 shifted_uv = (UV * scale_multiplier) + (scroll_speed * TIME);
    
    // Sample the color of your symbol using the shifted UV vectors
    COLOR = texture(TEXTURE, shifted_uv);
}
