shader_type spatial;

uniform float var : hint_range(0.0, 1.0);

void fragment() {
    // Texture Coordinate Object (object space coords)
	vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz - NODE_POSITION_WORLD;
	vec3 obj_pos = (inverse(MODEL_MATRIX) * vec4(world_pos, 1.0)).xyz * 100.0;
	world_pos = vec3(world_pos.x, -world_pos.z, world_pos.y);

    float length_tco = length(obj_pos);
	float length_moved = clamp(length_tco - var,0.0,1.0);
    float value1 = length_moved / (1.0 - var);
    float fm = mod(value1 * 8.0, 1.0); // Floored Modulo step

    float mod_flip = (-1.0 * fm) + 1.0;
    float multiply_labeled = fm * mod_flip;
    float multiplied2 = pow(multiply_labeled * 4.0, 2.0);

    // Dot product for rotation angle
    float dot_tco_pos = dot(obj_pos, world_pos);
    float rotation_angle = dot_tco_pos * 0.1f;

    // Rotate 'position' around Y axis by 'rotation_angle'
    float s = sin(rotation_angle);
    float c = cos(rotation_angle);

	 // Rotation Part
    vec3 rotated_vector = world_pos;

    rotated_vector.x = c * world_pos.x - s * world_pos.y;
	rotated_vector.y = s * world_pos.x + c * world_pos.y;
	rotated_vector.z = world_pos.z; // Z remains unchanged


    // After rotation
    float dot_neg1_1_0 = dot(rotated_vector, vec3(-1.0, 1.0, 0.0));
    float dot_1_0_0   = dot(rotated_vector, vec3(1.0, 0.0, 0.0));

    float multiply_labeled_3 = dot_neg1_1_0 * dot_1_0_0;

    float inv_value1 = 1.0 - value1;
    float multiply_labeled_1 = (inv_value1 * 4.0) + 1.0f;


    float combined = multiply_labeled_3 * multiply_labeled_1 * multiplied2;

    float final = (combined * 2.0) - 0.2;

    if (final > 0.0) {
        ALBEDO = vec3(1.0, 1.0, 1.0); // White where condition met
    } else {
        ALBEDO = vec3(0.0); // Black elsewhere
    }
}
