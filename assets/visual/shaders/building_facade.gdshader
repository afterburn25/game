shader_type spatial;
render_mode diffuse_burley;
uniform vec4 wall_color : source_color = vec4(.24,.29,.32,1);
uniform vec4 glass_color : source_color = vec4(.045,.11,.16,1);
uniform float building_seed=1.0;
uniform float window_light=.10;
varying vec3 facade_position;
varying vec3 facade_normal;
float hash(vec2 p){ return fract(sin(dot(p,vec2(127.1,311.7))+building_seed)*43758.5453); }
void vertex(){ facade_position=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz; facade_normal=normalize(mat3(MODEL_MATRIX)*NORMAL); }
void fragment(){
    vec2 p=vec2(abs(facade_normal.x)>.5?facade_position.z:facade_position.x,facade_position.y);
    vec2 grid=p/vec2(1.45,3.2), cell=floor(grid), f=fract(grid);
    vec2 aa=max(fwidth(grid),vec2(.004));
    float pane=smoothstep(.09-aa.x,.09+aa.x,f.x)*(1.0-smoothstep(.89-aa.x,.89+aa.x,f.x))*
        smoothstep(.12-aa.y,.12+aa.y,f.y)*(1.0-smoothstep(.93-aa.y,.93+aa.y,f.y));
    pane*=1.0-smoothstep(.5,.8,abs(facade_normal.y));
    float variation=hash(cell)*.25+.78;
    vec3 glass=glass_color.rgb*variation;
    glass+=vec3(.09,.17,.21)*smoothstep(.25,.90,f.y)*.04;
    ALBEDO=mix(wall_color.rgb*(.90+hash(floor(p*8.0))*.10),glass,pane);
    ROUGHNESS=mix(.67,.21,pane); METALLIC=mix(.30,.56,pane); SPECULAR=.6;
    float occupied=step(.57,hash(cell+vec2(8,19)));
    EMISSION=vec3(1.0,.73,.43)*pane*occupied*window_light;
}
