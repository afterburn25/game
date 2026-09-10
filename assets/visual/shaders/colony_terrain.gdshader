shader_type spatial;
render_mode diffuse_burley;

varying vec3 terrain_position;
varying vec3 terrain_normal;
uniform vec3 terrain_low : source_color = vec3(0.105,0.17,0.08);
uniform vec3 terrain_high : source_color = vec3(0.28,0.32,0.15);
uniform vec3 terrain_exposed_low : source_color = vec3(0.22,0.16,0.10);
uniform vec3 terrain_exposed_high : source_color = vec3(0.39,0.30,0.19);
uniform float terrain_seed = 0.0;
uniform float terrain_roughness = 0.97;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i=floor(p), f=fract(p); f=f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1.0,0.0)),f.x),
        mix(hash(i+vec2(0.0,1.0)),hash(i+vec2(1.0,1.0)),f.x),f.y);
}
void vertex() { terrain_position=VERTEX; terrain_normal=NORMAL; }
void fragment() {
    vec2 p=terrain_position.xz;
    vec2 world_seed=vec2(terrain_seed*173.7,terrain_seed*311.9);
    float broad=noise(p*0.012+world_seed)*0.6+noise(p*0.054+world_seed.yx)*0.4;
    float fine=noise(p*0.68+world_seed*2.7);
    float mottled=noise(p*0.14+vec2(17.0,41.0)+world_seed*0.43);
    vec3 grass=mix(terrain_low,terrain_high,broad);
    vec3 dirt=mix(terrain_exposed_low,terrain_exposed_high,broad);
    float clearing=1.0-smoothstep(34.0,78.0,length(p));
    float rocky=smoothstep(0.06,0.24,1.0-terrain_normal.y);
    vec3 base=mix(grass,dirt,max(clearing*0.82,rocky));
    float district_center=1.0-smoothstep(18.0,92.0,length(p));
    float district_wear=0.94+mottled*0.06;
    ALBEDO=base*(0.76+fine*0.18)*mix(1.0,district_wear,district_center*0.55);
    ROUGHNESS=terrain_roughness;
}
