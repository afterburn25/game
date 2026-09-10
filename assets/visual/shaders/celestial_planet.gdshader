shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform sampler2D surface_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform bool has_source = false;
uniform bool known_surface = false;
uniform bool is_gas = false;
uniform bool is_ocean = false;
uniform vec4 source_disc = vec4(0.0);
uniform vec4 surface_color : source_color = vec4(0.22, 0.28, 0.32, 1.0);
uniform vec4 atmosphere_color : source_color = vec4(0.20, 0.48, 0.85, 1.0);
uniform float atmosphere_strength : hint_range(0.0, 0.5) = 0.0;
uniform vec3 light_direction = vec3(-0.7, -0.3, 0.64);
uniform float surface_seed = 0.0;

float material_noise(vec3 p) {
    return 0.50 + 0.25 * sin(p.x * 8.0 + sin(p.z * 5.0) + surface_seed)
        * sin(p.y * 11.0 - surface_seed)
        + 0.12 * sin(dot(p, vec3(24.0, 19.0, 17.0)) + surface_seed);
}

void fragment() {
    vec2 p = (UV * 2.0 - 1.0) * 1.08;
    float radius = length(p);
    float edge = max(fwidth(radius), 0.0008);
    float coverage = 1.0 - smoothstep(1.0 - edge, 1.0, radius);
    if (radius >= 1.0) {
        float halo = exp(-max(radius-1.0,0.0)*65.0)*(1.0-smoothstep(1.0,1.078,radius));
        float sunrim = smoothstep(-.35,.55,dot(vec3(p/max(radius,.001),.05),normalize(light_direction)));
        COLOR = vec4(atmosphere_color.rgb,halo*sunrim*atmosphere_strength*.85)*COLOR;
    } else {
        vec3 normal = vec3(p, sqrt(max(0.0, 1.0 - dot(p, p))));
        vec3 light = normalize(light_direction);
        vec3 albedo = surface_color.rgb;
        if (has_source) {
            vec2 source_uv;
            if (source_disc.z > 0.0) {
                // Preserve the observed hemisphere of globe photographs, including
                // the documented LEFT natural-color Uranus comparison disc.
                source_uv = source_disc.xy + p * source_disc.zw;
            } else {
                source_uv = vec2(0.5 + atan(normal.x, normal.z) / 6.28318530718,
                    0.5 + asin(clamp(normal.y, -1.0, 1.0)) / 3.14159265359);
            }
            albedo = texture(surface_texture, source_uv).rgb;
        } else if (known_surface) {
            // Decorative class-level shading only; never used for unknown worlds.
            float variation = is_gas
                ? 0.72 + 0.28 * sin(normal.y * 65.0 + material_noise(normal * 4.0) * 8.0)
                : 0.58 + material_noise(normal) * 0.54 + material_noise(normal * 6.0) * 0.14;
            albedo *= variation;
        }

        float incidence = dot(normal, light);
        float daylight = smoothstep(-0.10, 0.16, incidence);
        float diffuse = max(incidence, 0.0);
        float illumination = 0.025 + daylight * (0.17 + diffuse * 0.91);
        vec3 lit = albedo * illumination;

        // Atmosphere uses only confirmed visual classes; the caller reserves an outer halo.
        float fresnel = pow(1.0 - normal.z, 3.0);
        float illuminated_rim = smoothstep(-0.2, 0.45, incidence);
        lit += atmosphere_color.rgb * atmosphere_strength * fresnel * illuminated_rim * 1.4;
        lit = mix(lit, atmosphere_color.rgb * 0.30,
            atmosphere_strength * fresnel * daylight * 0.25);

        // Subtle reflected-light glint on photographed blue ocean pixels.
        if (is_ocean && has_source) {
            float water = smoothstep(0.015, 0.12, albedo.b - max(albedo.r, albedo.g * 0.90));
            vec3 half_vector = normalize(light + vec3(0.0, 0.0, 1.0));
            float reflection = pow(max(dot(normal, half_vector), 0.0), 82.0);
            lit += vec3(0.72, 0.83, 0.92) * reflection * water * daylight * 0.22;
        }
        COLOR = vec4(lit, coverage) * COLOR;
    }
}
