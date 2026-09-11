shader_type spatial;
render_mode unshaded;
uniform vec4 star_color : source_color = vec4(1,.65,.2,1);
uniform float seed = 1.;
varying vec3 sphere_point;
float hash(vec3 p){ return fract(sin(dot(p,vec3(127.1,311.7,74.7)))*43758.5453); }
float noise(vec3 p){
    vec3 i=floor(p),f=fract(p); f=f*f*(3.-2.*f);
    return mix(mix(mix(hash(i),hash(i+vec3(1,0,0)),f.x),mix(hash(i+vec3(0,1,0)),hash(i+vec3(1,1,0)),f.x),f.y),
        mix(mix(hash(i+vec3(0,0,1)),hash(i+vec3(1,0,1)),f.x),mix(hash(i+vec3(0,1,1)),hash(i+vec3(1,1,1)),f.x),f.y),f.z);
}
void vertex(){sphere_point=normalize(VERTEX);}
void fragment(){
    vec3 drift=vec3(TIME*.013,-TIME*.009,TIME*.006);
    float warp=noise(sphere_point*9.7+vec3(seed*.031));
    float granules=noise(sphere_point*118.+drift+warp*3.1);
    float cells=noise(sphere_point*51.-drift*.7+warp*1.8);
    float convection=noise(sphere_point*17.+vec3(seed*.071));
    // Two differently scaled fields make sparse, ragged umbra/penumbra groups.
    // The threshold drifts slowly so individual regions form and erode rather than
    // reading as a fixed polka-dot texture.
    float spot_field=noise(sphere_point*5.9+vec3(seed*.137,seed*.053,TIME*.0025));
    float spot_detail=noise(sphere_point*13.7+vec3(seed*.019)-drift*.08);
    float spot_birth=.025*sin(TIME*.021+seed);
    float umbra=smoothstep(.815+spot_birth,.925+spot_birth,spot_field)*smoothstep(.43,.76,spot_detail);
    float penumbra=smoothstep(.735+spot_birth,.87+spot_birth,spot_field)*smoothstep(.31,.66,spot_detail);
    float rim=pow(1.-max(dot(normalize(NORMAL),normalize(VIEW)),0.),2.4);
    float limb=.68+.32*pow(max(dot(normalize(NORMAL),normalize(VIEW)),0.),.34);
    vec3 plasma=mix(star_color.rgb,vec3(1.,.965,.86),.16+granules*.28+cells*.11);
    float magnetic_darkening=1.-penumbra*.34-umbra*.48;
    ALBEDO=plasma*(.52+granules*.28+convection*.20)*limb*magnetic_darkening + star_color.rgb*rim*.24;
    EMISSION=ALBEDO*(.82+rim*.28);
}
