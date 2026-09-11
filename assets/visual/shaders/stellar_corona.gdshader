shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, fog_disabled;
uniform vec4 star_color : source_color = vec4(1.,.7,.3,1.);
uniform float seed = 1.;
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
    float flares=0.;
    // Independent, seeded magnetic loops emerge, arc above the limb and fade.
    // Each cycle chooses a new bearing; most of the time only the quiet corona remains.
    for(int i=0;i<3;i++){
        float period=13.+float(i)*5.7;
        float epoch=floor(TIME/period+hash(vec2(seed,float(i)*7.1)));
        float life=fract(TIME/period+hash(vec2(seed,float(i)*7.1)));
        float envelope=smoothstep(.05,.24,life)*(1.-smoothstep(.58,.94,life));
        float active=step(.47,hash(vec2(epoch+seed,float(i)+19.3)));
        float bearing=(hash(vec2(epoch*3.17+seed,float(i)*11.9))-.5)*6.2831853;
        float width=.20+.18*hash(vec2(epoch+4.2,float(i)+seed));
        float da=abs(atan(sin(angle-bearing),cos(angle-bearing)));
        float along=clamp(da/width,0.,1.);
        float loop_radius=.435+sin(along*3.1415926)*(.12+.12*life);
        float loop=exp(-abs(radius-loop_radius)*115.)*(1.-smoothstep(width*.72,width,da));
        flares+=loop*envelope*active;
    }
    ALBEDO=star_color.rgb;
    EMISSION=star_color.rgb*(.4+flares*7.5);
    ALPHA=(halo+streamers+flares*.92)*edge;
}
