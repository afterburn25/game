shader_type spatial;
render_mode diffuse_burley;

varying vec3 terrain_position;
varying vec3 terrain_normal;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i=floor(p), f=fract(p); f=f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1.0,0.0)),f.x),
        mix(hash(i+vec2(0.0,1.0)),hash(i+vec2(1.0,1.0)),f.x),f.y);
}
void vertex() { terrain_position=VERTEX; terrain_normal=NORMAL; }
void fragment() {
    vec2 p=terrain_position.xz;
    float broad=noise(p*0.012)*0.6+noise(p*0.054)*0.4;
    float fine=noise(p*0.68);
    vec3 grass=mix(vec3(0.105,0.17,0.08),vec3(0.28,0.32,0.15),broad);
    vec3 dirt=mix(vec3(0.22,0.16,0.10),vec3(0.39,0.30,0.19),broad);
    float clearing=1.0-smoothstep(34.0,78.0,length(p));
    float rocky=smoothstep(0.06,0.24,1.0-terrain_normal.y);
    vec3 base=mix(grass,dirt,max(clearing*0.82,rocky));
    ALBEDO=base*(0.87+fine*0.23);
    ROUGHNESS=0.97;
}
