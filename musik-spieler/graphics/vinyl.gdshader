shader_type spatial;

uniform float var : hint_range(0.0, 1.0);

uniform sampler2D NOISE_PATTERN;

float atan2(in float y, in float x)
{
    bool s = (abs(x) > abs(y));
    return mix(PI/2.0f - atan(x,y), atan(y,x), 1);
}

float radial_gradient_texture(in float x, in float y){
	return atan2(x,y)/(2.0f*PI) + 0.5f;
}

void fragment() {
    // Texture Coordinate Object (object space coords)
	vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz - NODE_POSITION_WORLD;
	vec3 obj_pos = (inverse(MODEL_MATRIX) * vec4(world_pos, 1.0)).xyz * 100.0;
	world_pos = vec3(world_pos.x, -world_pos.z, world_pos.y);
	obj_pos = vec3(obj_pos.x*0.01, -obj_pos.z*0.01, obj_pos.y*0.01);

    float length_tco = length(obj_pos);
	float length_moved = clamp(length_tco - var,0.0,1.0);
    float length_normalised = length_moved / (1.0 - var);
    float fm = mod(length_normalised * 8.0, 1.0); // Floored Modulo step

    float mod_flip = (-1.0 * fm) + 1.0;
    float multiply_labeled = fm * mod_flip;
    float multiplied2 = pow(multiply_labeled * 4.0, 2.0);

    // Dot product for rotation angle
    float dot_tco_pos = dot(obj_pos, world_pos);
    float rotation_angle = dot_tco_pos * 0.05f;

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

    float inv_length_normalised = 1.0 - length_normalised;
    float multiply_labeled_1 = (inv_length_normalised * 4.0) + 1.0f;


    float combined = multiply_labeled_3 * multiply_labeled_1 * multiplied2;

    float final = (combined * 2.0) - 0.2;

    if (final > 0.0) {
        ALBEDO = vec3(0.5); // White where conditlength_normalisedion met
    } else {
		float rg1 = radial_gradient_texture(obj_pos.x,obj_pos.y);
		float rg2 = radial_gradient_texture(-obj_pos.x,-obj_pos.y);
        float noiseValue1 = texture(NOISE_PATTERN, vec2(0.2)*vec2(rg1,length_normalised)).x;
		float noiseValue2 = texture(NOISE_PATTERN, vec2(0.2)*vec2(rg2,length_normalised)).x;
    	float factor = clamp(-4.0f*obj_pos.y, 0.0, 1.0);
		float mixedNoise = mix(noiseValue1, noiseValue2, factor);
		ALBEDO = vec3(mixedNoise)*0.0005+0.001;
    }
}
