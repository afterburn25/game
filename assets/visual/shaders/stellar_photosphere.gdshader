shader_type spatial;
render_mode unshaded;
uniform vec4 star_color : source_color = vec4(1,.65,.2,1);
varying vec3 sphere_point;
float hash(vec3 p){ return fract(sin(dot(p,vec3(127.1,311.7,74.7)))*43758.5453); }
float noise(vec3 p){
    vec3 i=floor(p),f=fract(p); f=f*f*(3.-2.*f);
    return mix(mix(mix(hash(i),hash(i+vec3(1,0,0)),f.x),mix(hash(i+vec3(0,1,0)),hash(i+vec3(1,1,0)),f.x),f.y),
        mix(mix(hash(i+vec3(0,0,1)),hash(i+vec3(1,0,1)),f.x),mix(hash(i+vec3(0,1,1)),hash(i+vec3(1,1,1)),f.x),f.y),f.z);
}
void vertex(){sphere_point=normalize(VERTEX);}
void fragment(){
    float granules=noise(sphere_point*110.+vec3(TIME*.013));
    float convection=noise(sphere_point*16.);
    float limb=.64+.36*pow(max(dot(normalize(NORMAL),normalize(VIEW)),0.),.4);
    ALBEDO=star_color.rgb*(.62+granules*.28+convection*.1)*limb;
    EMISSION=ALBEDO*.7;
}
