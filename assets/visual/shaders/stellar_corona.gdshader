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
    float halo=exp(-max(0.,radius-.425)*8.3)*.25;
    float outer_aura=exp(-max(0.,radius-.43)*3.7)*.055;
    float streamers=pow(max(0.,1.-radius),3.)*rays*.22;
    float edge=1.-smoothstep(.83,1.,radius);
    // The photosphere is a separate depth-tested sphere. Never paint radial corona
    // streamers through the billboard interior when a close camera views it off-axis.
    float outside_limb=smoothstep(.405,.455,radius);
    // One seeded eruption opportunity per long slot leaves genuinely quiet intervals.
    // The envelope is already zero at both slot boundaries, so a new random event cannot flicker in.
    float slot_length=9.5+hash(vec2(seed,4.7))*3.5;
    float shifted_time=TIME+hash(vec2(seed,17.1))*slot_length;
    float epoch=floor(shifted_time/slot_length);
    float slot_phase=fract(shifted_time/slot_length);
    float event_roll=hash(vec2(seed*2.31+epoch,31.7));
    float previous_roll=hash(vec2(seed*2.31+epoch-1.,31.7));
    // Random slots may be skipped, but never two in a row; a real-time visual check
    // therefore observes an eruption within a bounded two-slot window.
    float eruption_enabled=max(step(.48,event_roll),1.-step(.48,previous_roll));
    float start=.08+hash(vec2(seed+epoch*1.17,8.3))*.12;
    float duration=.29+hash(vec2(seed*3.07,epoch+12.4))*.20;
    float life=clamp((slot_phase-start)/duration,0.,1.);
    float growth=smoothstep(0.,.28,life);
    float fade=1.-smoothstep(.58,1.,life);
    float envelope=growth*fade*eruption_enabled;

    float bearing=(hash(vec2(epoch*3.17+seed,19.3))-.5)*6.2831853;
    bearing+=(hash(vec2(seed,epoch+2.9))-.5)*.13*sin(life*3.1415926);
    float half_width=.12+.12*hash(vec2(epoch+4.2,seed));
    float height=.11+.22*hash(vec2(seed*1.9,epoch+7.6));
    float size=.72+.45*hash(vec2(epoch+13.8,seed*.71));
    float da=abs(atan(sin(angle-bearing),cos(angle-bearing)));
    float along=clamp(da/half_width,0.,1.);
    float loop_radius=.435+sin(along*3.1415926)*height*growth;
    float ragged=.76+.24*noise(vec2(da*34.+epoch,TIME*.075+seed));
    float loop=exp(-abs(radius-loop_radius)*(92./size))
        *(1.-smoothstep(half_width*.72,half_width,da))*ragged;
    // A compact knot lifts away after the loop opens, making an eruption read as a spew
    // rather than a continuously pulsing decorative arc.
    float knot_radius=.45+height*(.24+life*1.12);
    float knot=exp(-pow((radius-knot_radius)/(.025+.014*size),2.))
        *exp(-pow(da/(half_width*.31),2.))*smoothstep(.16,.34,life)*fade;
    float flares=(loop+knot*1.35)*envelope;
    ALBEDO=star_color.rgb;
    EMISSION=star_color.rgb*(.4+flares*7.5);
    ALPHA=(halo+outer_aura+streamers+flares*.92)*edge*outside_limb;
}
