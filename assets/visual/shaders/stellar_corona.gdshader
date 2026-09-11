shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, fog_disabled;
uniform vec4 star_color : source_color = vec4(1.,.7,.3,1.);
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.-2.*f);return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);}

void vertex(){
    // Camera-facing geometry with a world-scale footprint, never a stretched image.
    MODELVIEW_MATRIX=VIEW_MATRIX*mat4(INV_VIEW_MATRIX[0],INV_VIEW_MATRIX[1],INV_VIEW_MATRIX[2],MODEL_MATRIX[3]);
}
void fragment(){
    vec2 q=(UV-.5)*2.;
    float radius=length(q);
    float angle=atan(q.y,q.x);
    float turbulence=noise(vec2(angle*3.2,TIME*.018)+q*4.);
    float rays=.68+.15*sin(angle*13.+TIME*.045)+.09*sin(angle*29.-TIME*.019)+.16*turbulence;
    float halo=exp(-max(0.,radius-.425)*9.5)*.19;
    float streamers=pow(max(0.,1.-radius),3.)*rays*.22;
    float edge=1.-smoothstep(.83,1.,radius);
    ALBEDO=star_color.rgb;
    EMISSION=star_color.rgb*.4;
    ALPHA=(halo+streamers)*edge;
}
