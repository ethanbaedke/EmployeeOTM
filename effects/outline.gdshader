shader_type canvas_item;

uniform vec4 outline_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float outline_width : hint_range(0.0, 20.0) = 1.0;
uniform bool outline_enabled = true;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);

    if (outline_enabled) {
        vec2 texel_size = outline_width / vec2(textureSize(TEXTURE, 0));

        // Sample alpha in 4 directions (and optionally diagonals)
        float alpha_up    = texture(TEXTURE, UV + vec2(0.0, -texel_size.y)).a;
        float alpha_down  = texture(TEXTURE, UV + vec2(0.0, texel_size.y)).a;
        float alpha_left  = texture(TEXTURE, UV + vec2(-texel_size.x, 0.0)).a;
        float alpha_right = texture(TEXTURE, UV + vec2(texel_size.x, 0.0)).a;

        float outline_alpha = clamp(alpha_up + alpha_down + alpha_left + alpha_right, 0.0, 1.0);

        // If current pixel is transparent but a neighbor isn't, draw outline.
        if (tex_color.a < 0.9 && outline_alpha > 0.0) {
            COLOR = vec4(outline_color.rgb, outline_alpha * outline_color.a);
        } else {
            COLOR = tex_color;
        }
    } else {
        COLOR = tex_color;
    }
}