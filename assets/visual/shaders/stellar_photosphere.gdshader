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
    float cells=noise(sphere_point*48.-vec3(TIME*.008));
    float convection=noise(sphere_point*16.);
    float rim=pow(1.-max(dot(normalize(NORMAL),normalize(VIEW)),0.),2.4);
    float limb=.68+.32*pow(max(dot(normalize(NORMAL),normalize(VIEW)),0.),.34);
    vec3 plasma=mix(star_color.rgb,vec3(1.,.965,.86),.18+granules*.26+cells*.10);
    ALBEDO=plasma*(.56+granules*.25+convection*.19)*limb + star_color.rgb*rim*.24;
    EMISSION=ALBEDO*(.82+rim*.28);
}
