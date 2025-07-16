shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

uniform vec4 albedo : source_color;
uniform float roughness : hint_range(0.0, 1.0);
uniform float specular : hint_range(0.0, 1.0, 0.01);
uniform float metallic : hint_range(0.0, 1.0, 0.01);

uniform mat4 box_transform;		//global transform matrix des box-objektes
uniform vec3 box_size;			//Size der Box

uniform vec4 fade_color : source_color;

uniform float transition_width = 0.1;

varying vec4 world_pos;

void vertex()
{
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0));
}

void fragment()
{
	vec3 local_pos = (inverse(box_transform) * world_pos).xyz;

	vec3 dst_to_edge = -(1. / transition_width) * (abs(local_pos) - (0.5 * box_size));
	float fade = clamp(min(dst_to_edge.x, min(dst_to_edge.y, dst_to_edge.z)), 0., 1.);

	if (fade == 0.0)
	{
		discard;
	}

	ALBEDO = mix(fade_color.rgb, albedo.rgb, fade);
	METALLIC = metallic;
	SPECULAR = specular;
	ROUGHNESS = roughness;
}