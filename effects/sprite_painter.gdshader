shader_type canvas_item;

uniform vec3 primary = vec3(1.0f, 0.0f, 0.0f);
uniform vec3 secondary = vec3(0.0f, 1.0f, 0.0f);
uniform vec3 tertiary = vec3(0.0f, 0.0f, 1.0f);

uniform vec4 modulation = vec4(1.0f, 1.0f, 1.0f, 1.0f);

void fragment() {
	vec4 originalColor = texture(TEXTURE, UV);
	vec3 finalColor = (primary * originalColor.r) + (secondary * originalColor.g) + (tertiary * originalColor.b);
	COLOR = vec4(finalColor, originalColor.a) * modulation;
}
