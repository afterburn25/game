shader_type canvas_item;
render_mode unshaded;

// Decorative light only. The campaign catalogue is drawn separately with authoritative
// positions, so no point in this shader represents a selectable system.
uniform vec2 aspect = vec2(1.0);
uniform float seed = 0.0;
uniform bool spiral = true;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7)) + seed * .01) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}
float fbm(vec2 p) { return noise(p) * .50 + noise(p * 2.03) * .25 + noise(p * 4.09) * .14 + noise(p * 8.21) * .07 + noise(p * 16.47) * .04; }

void fragment() {
    // r=1 is the visual edge of the fixed campaign galaxy frame.
    vec2 p = (UV - .5) * aspect / .40909091;
    // GalaxySpatialLayout compresses generated spiral coordinates on Y by .72. Match that
    // presentation transform here so playable outer-arm systems reach the same visible edge.
    if (spiral) p.y /= .72;
    float r = length(p), a = atan(p.y, p.x);
    float broad = fbm(p * 2.7 + vec2(seed * .0003));
    float clumps = fbm(p * 13.0);
    float grain = fbm(p * 56.0);
    // The coordinate generator uses angle = arm*pi/2 + radius*pi*2.35.
    // Use that same winding and radial scale; noise adds dust detail, not a different layout.
    float armPhase = 4.0 * (a - r * 7.38274274 + (broad - .5) * .14);
    // Broad noisy volumes avoid traced spiral-line silhouettes.
    float armField = pow(.5 + .5 * cos(armPhase), 1.35);
    float armVolume = armField * (.32 + .68 * smoothstep(.28, .80, clumps));
    float disk = exp(-r * r * 2.35) * (.27 + .16 * broad);
    float armEnvelope = smoothstep(.035, .16, r) * (1.0 - smoothstep(.88, 1.03, r));
    float density = disk + (spiral ? armVolume : clumps * .35) * armEnvelope * .65;

    float dustPhase = 4.0 * (a - r * 7.38274274 + (fbm(p * 5.6) - .5) * .18) + .46;
    float dust = pow(.5 + .5 * cos(dustPhase), 8.0) * armEnvelope;
    density *= 1.0 - (spiral ? dust : broad * armEnvelope) * (.30 + .15 * broad);
    density *= .82 + grain * .24;
    float core = exp(-r * r * 74.0);
    float bulge = exp(-r * r * 15.0) * (.17 + .08 * fbm(p * 18.0));
    density = (density + bulge) * (1.0 - smoothstep(.95, 1.06, r));

    // RGB is a tint and alpha carries density: avoiding the previous double density multiply.
    vec3 ivory = mix(vec3(.42, .48, .55), vec3(.78, .77, .71), .34 + grain * .34);
    float warmMix = clamp((core * .52 + bulge * .10) / max(density, .001), 0.0, .36);
    vec3 colour = mix(ivory, vec3(.86, .82, .71), warmMix);
    vec2 cells = floor(p * 185.0);
    float micro = step(.9915, hash(cells)) * smoothstep(.18, .72, density) * (1.0 - smoothstep(.95, 1.06, r));
    colour = mix(colour, vec3(1.0, .96, .84), micro * .50);
    float alpha = clamp(density * .92 + core * .12 + micro * .18, 0.0, .78);
    COLOR = vec4(colour, alpha);
}
