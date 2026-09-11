shader_type canvas_item;
render_mode unshaded;

// This is a single, bounded decorative pass. Strategic systems are drawn separately
// from their saved catalogue positions; no speck below is a selectable system.
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
    // r=1 is the edge of the fixed campaign galaxy frame. The Y compensation matches
    // GalaxySpatialLayout so the full playable four-arm catalogue stays inside the light.
    vec2 p = (UV - .5) * aspect / .40909091;
    if (spiral) p.y /= .72;
    float r = length(p), a = atan(p.y, p.x);
    float broad = fbm(p * 2.15 + vec2(seed * .0003));
    float cloud = fbm(p * 5.5 + vec2(3.7, 8.1));
    float filament = fbm(p * 13.5 + vec2(11.3, 2.9));
    float grain = fbm(p * 48.0);

    // The generator uses four arms and radius * 2.35 turns. Offset, widen and break
    // those centers so the result is a single stellar disc with embedded arm structure.
    float winding = 4.0 * (a - r * 7.38274274 + (broad - .5) * 1.05 + (cloud - .5) * .44);
    float armWave = .5 + .5 * cos(winding);
    float armRidge = smoothstep(.00, .73, armWave);
    float armShelf = smoothstep(.00, .66, .5 + .5 * cos(winding + (filament - .5) * 1.35));
    float breaks = smoothstep(.28, .77, cloud) * (.60 + .40 * smoothstep(.25, .80, broad));
    float armMass = .13 + armRidge * (.26 + .38 * breaks) + armShelf * (.12 + .16 * broad);
    float disc = exp(-r * r * 1.12) * (.30 + .28 * broad + .19 * cloud);
    float envelope = smoothstep(.018, .13, r) * (1.0 - smoothstep(.84, 1.045, r));
    float outerFade = 1.0 - smoothstep(.72, 1.02, r);
    float density = disc + (spiral ? armMass : cloud * .44) * envelope * (.64 + .36 * outerFade);

    // Dark, irregular dust lanes cut across the shared mass rather than outlining each arm.
    float dustWinding = .5 + .5 * cos(winding + .42 + (filament - .5) * .82);
    float dustFilament = smoothstep(.52, .78, filament) * smoothstep(.30, .73, dustWinding);
    float dustGaps = smoothstep(.57, .82, fbm(p * 8.0 + vec2(18.3, 6.2)));
    density *= 1.0 - (dustFilament * .23 + dustGaps * .09) * envelope;

    // A lopsided barred core makes a physical visual anchor without changing navigation.
    vec2 barP = p * mat2(vec2(.83, .56), vec2(-.56, .83));
    float bar = exp(-dot(barP * vec2(1.0, 2.9), barP * vec2(1.0, 2.9)) * 7.0);
    float core = exp(-r * r * 34.0);
    float bulge = exp(-r * r * 7.4) * (.30 + .22 * fbm(p * 15.0));
    float knots = smoothstep(.69, .85, fbm(p * 22.0 + vec2(4.0, 9.0))) * (.025 + armRidge * .095);
    density = (density + bulge + bar * .22 + knots) * (1.0 - smoothstep(.94, 1.07, r));
    density *= .84 + grain * .22;

    // Warm ivory is constrained to the bulge; arm light stays cool slate-blue and subdued.
    vec3 cool = mix(vec3(.32, .40, .50), vec3(.57, .65, .73), .35 + grain * .28);
    vec3 warm = mix(vec3(.70, .61, .48), vec3(.98, .82, .58), .54 + grain * .22);
    float warmMix = clamp(core * .84 + bulge * .34 + bar * .10, 0.0, .86);
    vec3 colour = mix(cool, warm, warmMix);
    vec2 cells = floor(p * 210.0);
    float micro = step(.991, hash(cells)) * smoothstep(.12, .62, density) * (1.0 - smoothstep(.94, 1.07, r));
    colour = mix(colour, vec3(.94, .96, 1.0), micro * .36);
    float alpha = clamp(density * .79 + core * .17 + micro * .08, 0.0, .76);
    COLOR = vec4(colour, alpha);
}
