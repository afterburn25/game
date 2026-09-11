shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform vec4 star_color : source_color = vec4(1.0, 0.80, 0.47, 1.0);

void fragment() {
    vec2 p = UV * 2.0 - 1.0;
    float radial = length(p);
    float photosphere = 0.455;
    float edge = max(fwidth(radial), 0.001);
    float disc_alpha = 1.0 - smoothstep(photosphere - edge, photosphere + edge, radial);
    float theta = atan(p.y, p.x);
    float slow_phase = TIME * 0.025;
    float streamers = 0.75 + 0.13 * sin(theta * 9.0 + slow_phase)
        + 0.09 * sin(theta * 17.0 - slow_phase * 0.6)
        + 0.06 * sin(theta * 31.0 + sin(theta * 5.0 - slow_phase));
    float corona_distance = max(0.0, radial - photosphere);
    float corona = exp(-corona_distance * 10.0) * streamers
        * (1.0 - smoothstep(0.68, 1.0, radial)) * 0.42;
    float prominence = exp(-corona_distance * 43.0) * 0.32;
    float granules = .94 + .035 * sin(p.x * 67.0 + sin(p.y * 43.0 + slow_phase))
        + .025 * sin(p.y * 91.0 - p.x * 38.0 - slow_phase * .7);
    float limb = sqrt(max(0.0, 1.0 - pow(radial / photosphere, 2.0)));
    vec3 core_color = mix(star_color.rgb, vec3(1.0, 0.975, 0.90), 0.42 + limb * 0.22)
        * (.78 + limb * .25) * granules;
    vec3 halo_color = star_color.rgb * (0.58 + prominence);
    float alpha = max(disc_alpha, corona);
    vec3 result = mix(halo_color, core_color, disc_alpha);
    COLOR = vec4(result, alpha) * COLOR;
}
