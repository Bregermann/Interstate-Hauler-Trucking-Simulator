"""Deliberately authored Truck Taxi bodies. Blender 3.5, metres, Unity +Z forward.

Original body meshes are exported separately from the unmodified licensed NWH
reference geometry. Source chassis/wheels appear only in local review renders.
"""
import bpy
import bmesh
import json
import math
import sys
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion

ROOT = Path(__file__).resolve().parent
MODELS = ROOT / 'Models'
EXPORTS = ROOT / 'Exports'
RENDERS = ROOT / 'Renders'
for folder in (MODELS, EXPORTS, RENDERS):
    folder.mkdir(parents=True, exist_ok=True)
REFERENCE = ROOT/'SourceReference/Starter_Untouched_Reference.blend'
LAYOUT = json.loads((ROOT/'SourceReference/PrefabLayout.json').read_text())
BYID = {r['id']:r for r in LAYOUT}
parts = []
M = {}

# Explicit art direction, not randomized geometry or runtime body generation.
DESIGNS = {
    'HighwayKing': dict(title='HIGHWAY KING', color=(.055,.23,.28,1), accent=(.72,.79,.78,1),
        hood=[(1.46,.91,1.05,2.18),(2.40,.87,1.02,2.15),(3.05,.78,.91,2.06)],
        roof=3.57, sleeper=3.89, rear=-.91, rearAxleOffset=-1.00, fifthWheelOffset=-1.20, style='classic', stacks=3.95),
    'Nightrunner': dict(title='NIGHTRUNNER', color=(.08,.19,.43,1), accent=(.45,.56,.65,1),
        hood=[(1.47,1.00,1.00,2.24),(1.78,1.01,.97,2.20),(2.12,.99,.91,2.10),(2.45,.94,.84,1.92),(2.78,.87,.79,1.73),(3.08,.80,.77,1.64)],
        roof=3.43, sleeper=3.96, rear=-.91, rearAxleOffset=-1.00, fifthWheelOffset=-1.20, style='aero', stacks=0),
    'Outlaw': dict(title='OUTLAW', color=(.21,.035,.055,1), accent=(.14,.16,.18,1),
        hood=[(1.47,.96,1.00,2.28),(2.32,.93,.88,2.21),(3.10,.82,.80,2.08)],
        roof=3.37, sleeper=0, rear=.04, fifthWheelOffset=-.25, style='custom', stacks=4.02),
    'Thunderhaul': dict(title='THUNDERHAUL', color=(.72,.16,.035,1), accent=(.08,.10,.12,1),
        hood=[(1.47,1.00,.97,2.20),(2.35,.94,.81,1.94),(3.10,.83,.70,1.68)],
        roof=3.15, sleeper=0, rear=.04, fifthWheelOffset=-.25, style='sport', stacks=0),
    'RoadPalace': dict(title='ROAD PALACE', color=(.26,.075,.29,1), accent=(.78,.66,.40,1),
        hood=[(1.47,.99,1.02,2.37),(2.46,.92,.97,2.33),(3.15,.85,.84,2.13)],
        roof=3.71, sleeper=4.12, rear=-1.50, rearAxleOffset=-1.59, fifthWheelOffset=-1.79, style='luxury', stacks=4.10),
}

def p(x,y,z):
    # Unity's left-handed +Z-forward frame to Blender's right-handed -Y-forward.
    return (-x,-z,y)

def mat(name, color, metal=0, rough=.4, emission=0, alpha=1):
    material=bpy.data.materials.new(name)
    material.diffuse_color=(*color[:3],alpha)
    material.use_nodes=True
    bs=material.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*color[:3],alpha)
    bs.inputs['Metallic'].default_value=metal
    bs.inputs['Roughness'].default_value=rough
    if emission:
        bs.inputs['Emission'].default_value=(*color[:3],1)
        bs.inputs['Emission Strength'].default_value=emission
    if alpha<1:
        bs.inputs['Alpha'].default_value=alpha
        material.blend_method='BLEND'
        material.use_screen_refraction=True
        material.show_transparent_back=False
    return material

def materials(d):
    M.clear()
    M.update(paint=mat('TT_Paint_Primary',d['color'],.55,.27),
        secondary=mat('TT_MetalTrim',d['accent'],.65,.26),
        chrome=mat('TT_Chrome',(.56,.62,.67,1),.94,.19),
        black=mat('TT_Chassis',(.025,.031,.036,1),.25,.5),
        rubber=mat('TT_Rubber',(.018,.023,.026,1),0,.85),
        glass=mat('TT_Glass',(.09,.18,.22,1),.25,.12,alpha=.23),
        interior=mat('TT_Interior',(.10,.12,.14,1),0,.72),
        fabric=mat('TT_Fabric',(.19,.25,.28,1),0,.95),
        bedding=mat('TT_Bedding',(.66,.74,.76,1),0,.9),
        wood=mat('TT_Cabinet',(.21,.115,.062,1),0,.56),
        warm=mat('TT_InteriorLamp',(1,.70,.35,1),.1,.25,.4),
        white=mat('TT_Headlamp',(.78,.88,1,1),.1,.15,.3),
        amber=mat('TT_MarkerLamp',(1,.34,.028,1),.2,.2,.3),
        red=mat('TT_TailLamp',(.65,.025,.018,1),.15,.25,.2))

def finish(obj,name,material='paint',bevel=0,smooth=False):
    obj.name=name
    obj.data.use_auto_smooth=True
    obj.data.materials.clear()
    obj.data.materials.append(M[material])
    if bevel:
        mod=obj.modifiers.new('Manufactured edge radii','BEVEL')
        mod.width=bevel
        mod.segments=3
    if smooth:
        for face in obj.data.polygons: face.use_smooth=True
    if bevel:
        normals=obj.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL')
        normals.keep_sharp=True
    parts.append(obj)
    return obj

def box(name,loc,dimensions,material='paint',bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p(*loc))
    obj=bpy.context.object
    obj.dimensions=(dimensions[0],dimensions[2],dimensions[1])
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(obj,name,material,bevel)

def mesh(name,vertices,faces,material='paint',bevel=.015,smooth=False):
    data=bpy.data.meshes.new(name)
    data.from_pydata([p(*v) for v in vertices],[],faces)
    data.update()
    obj=bpy.data.objects.new(name,data)
    bpy.context.scene.collection.objects.link(obj)
    return finish(obj,name,material,bevel,smooth)

def panel(name,points,material='paint',thickness=.035):
    obj=mesh(name,points,[tuple(range(len(points)))],material,0)
    sol=obj.modifiers.new('Panel gauge','SOLIDIFY'); sol.thickness=thickness
    edge=obj.modifiers.new('Panel edge radii','BEVEL'); edge.width=.012; edge.segments=2
    return obj

def pipe(name,points,radius,material='chrome'):
    curve=bpy.data.curves.new(name,'CURVE'); curve.dimensions='3D'
    curve.resolution_u=8; curve.bevel_resolution=3; curve.bevel_depth=radius
    spline=curve.splines.new('POLY'); spline.points.add(len(points)-1)
    for pt,value in zip(spline.points,points): pt.co=(*p(*value),1)
    obj=bpy.data.objects.new(name,curve); bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(M[material]); parts.append(obj)
    return obj

def cyl(name,loc,radius,depth,material='chrome',axis='y',vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=p(*loc))
    obj=bpy.context.object
    if axis=='x': obj.rotation_euler[1]=math.pi/2
    elif axis=='z': obj.rotation_euler[0]=math.pi/2
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(obj,name,material,.012,True)

def loft(name,rings,material='paint',bevel=.045,smooth=False):
    vertices=[]
    for z,w,bottom,top in rings:
        c=min(.12,(top-bottom)*.2)
        vertices.extend([(-w+c,bottom,z),(w-c,bottom,z),(w,bottom+c,z),(w,top-c,z),
            (w-c,top,z),(-w+c,top,z),(-w,top-c,z),(-w,bottom+c,z)])
    faces=[tuple(reversed(range(8))),tuple(range((len(rings)-1)*8,len(rings)*8))]
    for i in range(len(rings)-1):
        for j in range(8): faces.append((i*8+j,i*8+(j+1)%8,(i+1)*8+(j+1)%8,(i+1)*8+j))
    return mesh(name,vertices,faces,material,bevel,smooth)

def aero_sleeper_roof(d):
    # Open-bottom curved shell: the raised roof is real interior volume, not a
    # solid box over the berth. Stations form a continuous cab-to-sleeper fairing.
    rear,roof=d['rear'],d['sleeper']
    stations=[(rear,roof-.20), (rear+.18,roof-.04),(-.38,roof),
              (.08,roof-.08),(.54,roof-.30),(1.19,d['roof']-.08)]
    vertices=[]
    for z,height in stations:
        vertices.extend([(x,y,z) for x,y in [(-1.17,3.06),(-1.16,height-.28),
            (-1.08,height-.11),(-.92,height-.035),(-.62,height),(0,height+.025),
            (.62,height),(.92,height-.035),(1.08,height-.11),(1.16,height-.28),(1.17,3.06)]])
    faces=[]
    for i in range(len(stations)-1):
        for j in range(10): faces.append((i*11+j,i*11+j+1,(i+1)*11+j+1,(i+1)*11+j))
    shell=mesh('Aerodynamic raised roof shell',vertices,faces,'paint',.015,True)
    thickness=shell.modifiers.new('Formed roof panel thickness','SOLIDIFY'); thickness.thickness=.045
    # Raised rear dome closes above the lower rear wall while keeping berth open.
    panel('Aerodynamic rear roof closure',vertices[:11],'paint',.045)

def fender(name,x,cy,cz,outer=.76,inner=.63,depth=.48,material='paint',angles=(-12,192)):
    # Annular strip: actual open wheel arch, never a solid box over the wheel.
    verts=[]; faces=[]; count=29
    for side in (-.5,.5):
        for radius in (inner,outer):
            for i in range(count):
                a=math.radians(angles[0]+(angles[1]-angles[0])*i/(count-1))
                verts.append((x+side*depth,cy+math.sin(a)*radius,cz+math.cos(a)*radius))
    for i in range(count-1):
        for a,b in ((0,count),(2*count,3*count),(0,2*count),(count,3*count)):
            faces.append((a+i,a+i+1,b+i+1,b+i))
    faces.extend([(0,count,3*count,2*count),(count-1,2*count-1,4*count-1,3*count-1)])
    return mesh(name,verts,faces,material,.018,True)

def label(text,loc,size=.1,rotation=(math.pi/2,0,0),material='chrome'):
    data=bpy.data.curves.new('Badge '+text,'FONT'); data.body=text; data.align_x='CENTER'
    data.size=size; data.extrude=.0015; data.bevel_depth=.0006
    obj=bpy.data.objects.new('Badge '+text,data); bpy.context.scene.collection.objects.link(obj)
    obj.location=p(*loc); obj.rotation_euler=rotation; obj.data.materials.append(M[material]); parts.append(obj)
    return obj

def cabin(d):
    top=d['roof']; front=1.47; rear=.10; width=1.15
    # Windshield opening is real geometry, surrounded by A-pillars and roof rails.
    for side in (-1,1):
        x=side*width
        panel('Door lower '+str(side),[(x,1.20,rear),(x,1.20,front),(x,2.13,front),(x,2.16,rear)])
        panel('Door rear pillar '+str(side),[(x,2.14,rear),(x,top-.1,rear),(x,top-.1,.36),(x,2.14,.36)])
        panel('Door upper rail '+str(side),[(x,top-.1,.35),(x,top-.1,1.15),(x,top-.29,1.46),(x,top-.38,.35)])
        panel('Side window '+str(side),[(x,2.18,.39),(x,top-.39,.39),(x,top-.39,1.12),(x,2.18,1.45)],'glass',.008)
        pipe('Window seals '+str(side),[(x,2.17,.38),(x,top-.38,.38),(x,top-.38,1.13),(x,2.17,1.46),(x,2.17,.38)],.018,'rubber')
        pipe('A pillar '+str(side),[(x,2.13,1.47),(side*1.07,top-.12,1.17)],.055,'paint')
        pipe('Door shutline '+str(side),[(x*1.006,1.29,.32),(x*1.006,2.09,.32),(x*1.006,2.09,1.39),(x*1.006,1.29,1.39),(x*1.006,1.29,.32)],.008,'rubber')
        box('Door handle '+str(side),(x*1.02,2.02,.48),(.035,.05,.22),'chrome',.014)
        # Two functional-looking perforated steps, with the wheel's swept volume left open.
        for y,z,length in ((.55,.50,1.12),(.87,.56,1.02)):
            box('Cab step '+str(side),(side*1.13,y,z),(.38,.10,length),'chrome',.025)
            for k in range(6):
                box('Step tread '+str(side),(side*1.13,y+.056,z-length*.4+k*length*.16),(.29,.008,.025),'rubber',.003)
        pipe('Mirror bracket '+str(side),[(side*1.10,2.30,1.35),(side*1.39,2.39,1.46),(side*1.46,2.64,1.34)],.024,'chrome')
        box('Mirror housing '+str(side),(side*1.46,2.54,1.32),(.20,.53,.15),'black',.07)
        glass=box('Mirror reflective face '+str(side),(side*1.46,2.54,1.237),(.16,.45,.012),'chrome',.04)
        glass['TruckTaxiBinding']='MirrorLeft' if side<0 else 'MirrorRight'
    panel('Windshield',[(-1.075,2.17,1.475),(1.075,2.17,1.475),(1.035,top-.14,1.18),(-1.035,top-.14,1.18)],'glass',.009)
    pipe('Windshield surround',[(-1.09,2.16,1.49),(1.09,2.16,1.49),(1.05,top-.12,1.185),(-1.05,top-.12,1.185),(-1.09,2.16,1.49)],.027,'rubber')
    pipe('Windshield divider',[(0,2.18,1.477),(0,top-.14,1.184)],.016,'black')
    box('Front cowl',(0,2.01,1.43),(2.24,.34,.17),'paint',.065)
    for x in (-.54,.54):
        pipe('Windshield wiper',[(x-.22,2.22,1.49),(x+.16,2.29,1.47)],.012,'rubber')
    loft('Cab roof cap',[(.06,1.16,top-.15,top),(1.13,1.13,top-.15,top),(1.27,1.04,top-.25,top-.12)],'paint',.065)
    box('Visor',(0,top-.22,1.30),(2.18,.12,.28),'secondary',.035)
    for x in (-.83,-.42,0,.42,.83):
        box('Roof marker base',(x,top+.02,.94),(.15,.055,.22),'black',.025)
        box('Roof amber marker',(x,top+.06,1.00),(.115,.055,.11),'amber',.025)
    if not d['sleeper']:
        panel('Cab rear wall',[(-1.14,1.18,.08),(1.14,1.18,.08),(1.14,top-.1,.08),(-1.14,top-.1,.08)],'paint')
    # Dashboard/chairs are authored geometry, not a solid opaque cabin extension.
    box('Interior floor',(0,1.16,.75),(2.23,.09,1.50),'interior',.035)
    loft('Instrument console',[(1.14,1.08,1.65,1.97),(1.49,1.08,1.78,2.10)],'interior',.035)
    for x in (-.57,.57):
        box('Seat pedestal',(x,1.40,.69),(.41,.43,.45),'black',.055)
        box('Seat cushion',(x,1.63,.72),(.52,.16,.57),'fabric',.075)
        chair=box('Seat backrest',(x,1.99,.47),(.53,.73,.16),'fabric',.075)
        chair.rotation_euler[0]=math.radians(-8)
        box('Seat head restraint',(x,2.39,.45),(.38,.22,.17),'fabric',.055)
    for x in (-.72,-.44):
        cyl('Instrument dial',(x,1.99,1.17),.09,.015,'black','z')
        for k in range(9):
            a=math.pi*(.1+k*.1)
            box('Gauge tick',(x+math.cos(a)*.073,1.99+math.sin(a)*.073,1.155),(.009,.018,.007),'white',.001)
        gauge='GaugeSpeed' if x<-.6 else 'GaugeRpm'
        needle=pipe('Instrument needle '+gauge,[(x,1.99,1.144),(x-.058,1.968,1.144)],.004,'red')
        needle['TruckTaxiBinding']=gauge
        hub=cyl('Instrument hub '+gauge,(x,1.99,1.142),.012,.009,'secondary','z',16)
        hub['TruckTaxiBinding']=gauge
    box('Central display',(.27,1.99,1.165),(.30,.18,.018),'black',.02)
    for x in (-.90,.86): box('Air vent',(x,1.99,1.16),(.15,.11,.025),'black',.015)
    bpy.ops.mesh.primitive_torus_add(major_segments=40,minor_segments=10,location=p(-.56,1.99,1.06),
        major_radius=.19,minor_radius=.023)
    wheel=bpy.context.object; wheel.rotation_euler[0]=math.radians(32)
    finish(wheel,'Steering wheel rim','rubber',0,True)
    cyl('Steering wheel hub',(-.56,1.99,1.06),.06,.07,'black')
    for angle in (0,120,240):
        a=math.radians(angle)
        pipe('Steering wheel spoke',[(-.56,1.99,1.06),(-.56+math.cos(a)*.17,1.99+math.sin(a)*.09,1.06+math.sin(a)*.145)],.016,'secondary')

def chassis(d):
    # Match the measured NWH wheelbase. These are original visual meshes; NWH
    # continues owning the real axles, wheels, fifth-wheel and collision setup.
    rear_offset=d.get('rearAxleOffset',0)
    for x in (-.48,.48):
        box('Chassis rail',(x,.64,-.05+rear_offset/2),(.14,.30,5.70-rear_offset),'black',.014)
    for z in (-2.65,-1.96,-.90,.1,1.12,2.35):
        box('Frame crossmember',(0,.64,z),(1.10,.15,.12),'black',.012)
    for z,y in ((1.561,.530),(-1.620+rear_offset,.630)):
        cyl('Axle housing',(0,y,z),.10,1.78,'black','x')
        center=box('Differential',(0,y,z),(.40,.29,.29),'black',.10)
    pipe('Driveshaft',[(0,.67,-1.62+rear_offset),(0,.68,.85)],.065,'black')
    hitch_offset=d.get('fifthWheelOffset',0)
    cyl('Fifth wheel pivot',(0,.88,-1.162+hitch_offset),.25,.13,'black')
    cyl('Fifth wheel plate',(0,.965,-1.162+hitch_offset),.48,.07,'black',vertices=48)
    box('Fifth wheel throat',(0,1.006,-1.50+hitch_offset),(.08,.01,.40),'chrome',.003)
    box('Rear light crossbar',(0,.59,-2.87+rear_offset),(2.02,.15,.13),'black',.02)
    for x in (-.92,.92):
        box('Rear stop lamp',(x,.61,-2.95+rear_offset),(.24,.08,.035),'red',.018)
        box('Rear turn lamp',(x-.25*(1 if x>0 else -1),.61,-2.95+rear_offset),(.14,.08,.035),'amber',.016)
    box('Rear registration plate',(0,.59,-2.955+rear_offset),(.45,.12,.018),'secondary',.008)
    # Deliberately different rim faces, later bound to the NWH rotating wheels.
    if d['style'] in ('classic','aero','sport','luxury','custom'):
        for tag,cx,cy,cz in (('FL',-1.069,.530,1.561),('FR',1.079,.530,1.561),('RL',-1.017,.630,-1.620),('RR',1.027,.630,-1.620)):
            if tag.startswith('R'): cz+=rear_offset
            cyl('Wheel_'+tag+'_rim',(cx,cy,cz),.30,.018,'secondary','x',40)
            cyl('Wheel_'+tag+'_hub',(cx*1.018,cy,cz),.115,.065,'chrome','x',32)
            for j in range(10 if d['style']=='sport' else 12):
                a=math.tau*j/(10 if d['style']=='sport' else 12)
                cyl('Wheel_'+tag+'_vent',(cx*1.01,cy+math.sin(a)*.23,cz+math.cos(a)*.23),.036,.008,'black','x',12)

def exterior(d):
    start=len(parts)
    cabin(d)
    for obj in parts[start:]: obj['TruckTaxiMount']='Cab'
    hood=[(z,w,b,t-.22) for z,w,b,t in d['hood']]
    loft('Sculpted hood',hood,'paint',.075 if d['style']=='aero' else .04,d['style']=='aero')
    nose=hood[-1]; z,w,b,t=nose
    frontz=z+.036
    if d['style'] in ('classic','luxury'):
        box('Grille surround',(0,(b+t)/2,frontz),(w*1.62,t-b-.10,.12),'chrome',.075)
        box('Grille recess',(0,(b+t)/2,frontz+.07),(w*1.42,t-b-.24,.035),'black',.04)
        for i in range(17):
            x=-w*.64+i*(w*1.28)/16
            box('Vertical grille blade',(x,(b+t)/2,frontz+.10),(.026,t-b-.28,.04),'chrome',.008)
    else:
        loft('Tapered grille',[(frontz+.03,w*.79,b+.06,t-.09),(frontz+.10,w*.69,b+.12,t-.16)],'black',.04)
        for i in range(7):
            y=b+.15+i*(t-b-.34)/6
            box('Grille horizontal blade',(0,y,frontz+.12),(w*1.24,.027,.025),'secondary',.008)
    label(d['title'],(0,t-.12,frontz+.14),.072)
    box('Front bumper',(0,.61,z+.07),(2.43,.30,.27),'secondary',.07)
    for x in (-.99,.99):
        box('Headlight bezel',(x,1.05 if t>1.9 else .91,z-.025),(.39,.28,.20),'black',.045)
        if d['style']=='classic':
            for delta in (-.095,.095): cyl('Round headlamp',(x+delta,1.055,z+.086),.074,.025,'white','z')
        else:
            box('LED headlamp',(x,1.08 if t>1.9 else .94,z+.086),(.32,.075,.025),'white',.025)
            box('Direction repeater',(x,.98 if t>1.9 else .84,z+.09),(.30,.034,.023),'amber',.013)
        box('Bumper foglamp',(x,.63,z+.216),(.19,.065,.025),'white',.018)
    for side in (-1,1):
        fender('Front swept wheel arch '+str(side),side*1.01,.55,1.56,outer=.64 if d['style']=='sport' else .62,inner=.51,depth=.46)
        fender('Rear wheel mudguard '+str(side),side*.87,.63,-1.62+d.get('rearAxleOffset',0),outer=.62,inner=.51,depth=.67,material='secondary')
        box('Rear mudflap',(side*.87,.36,-2.38+d.get('rearAxleOffset',0)),(.62,.62,.035),'rubber',.012)
        cyl('Fuel tank',(side*.92,.68,-.12),.31,1.27,'secondary','z',48)
        for zband in (-.57,.32):
            cyl('Fuel tank band',(side*.92,.68,zband),.318,.065,'chrome','z',48)
        cyl('Fuel filler',(side*1.19,.88,.20),.07,.035,'chrome','x')
        if d['stacks']:
            x=side*1.20
            pipe('Exhaust downpipe '+str(side),[(side*.68,.50,-.10),(x,.72,-.09),(x,1.20,.06)],.073,'chrome')
            cyl('Exhaust heat shield',(x,1.88,.06),.122,1.42,'secondary','y',40)
            cyl('Tall exhaust stack',(x,(2.57+d['stacks'])/2,.06),.085,d['stacks']-2.57,'chrome','y',40)
            cyl('Exhaust dark outlet',(x,d['stacks']+.004,.06),.069,.012,'black','y')
            for j in range(9): box('Shield perforation',(x+side*.12,1.34+j*.12,.065),(.008,.05,.075),'black',.012)
        if d['style'] in ('aero','sport','luxury'):
            loft('Side skirt '+str(side),[(-.89,.16,.32,.91),(.39,.16,.34,.96)],'paint',.07).location.x=side*1.06
        for zmark in (-.68,.05,.65):
            box('Side amber marker',(side*1.23,.98,zmark),(.016,.036,.11),'amber',.01)
    if d['style']=='custom':
        pipe('Bumper bullbar',[(-1.13,.67,z+.30),(-1.13,1.18,z+.30),(1.13,1.18,z+.30),(1.13,.67,z+.30)],.055,'chrome')
        for x in (-.85,-.42,0,.42,.85): cyl('Roof rally lamp',(x,d['roof']+.14,.71),.105,.14,'white','z')
        for side in (-1,1):
            pipe('Side slash trim',[(side*1.17,1.45,.37),(side*1.17,1.34,1.04)],.024,'secondary')
    if d['style']=='sport':
        panel('Front splitter',[(-1.25,.43,z-.10),(1.25,.43,z-.10),(1.22,.43,z+.31),(-1.22,.43,z+.31)],'black',.035)
        for x in (-.72,.72):
            box('Roof aero support',(x,d['roof']+.07,.19),(.055,.19,.18),'black',.012)
        box('Roof aero lip',(0,d['roof']+.17,.19),(2.25,.055,.38),'secondary',.025)
        for side in (-1,1):
            for k in range(4):
                box('Hood heat extractor',(side*.96,1.83,1.75+k*.12),(.04,.12,.057),'black',.015)
    if d['style']=='luxury':
        for y in (b+.24,(b+t)/2,t-.19):
            box('Palace grille bridge',(0,y,frontz+.13),(w*1.46,.035,.025),'secondary',.008)
        for side in (-1,1):
            pipe('Palace coachline',[(side*1.18,1.43,-1.40),(side*1.18,1.43,.12),(side*1.18,1.56,.88)],.025,'secondary')
            box('Palace skirt sill',(side*1.10,.46,-.55),(.28,.20,2.25),'secondary',.055)
            for zmark in (-1.30,-.90,-.50):
                box('Palace side jewel',(side*1.18,.61,zmark),(.025,.055,.12),'amber',.015)
        box('Palace upper light bar',(0,3.72,1.10),(1.72,.07,.08),'secondary',.025)
        for x in (-.72,-.48,-.24,0,.24,.48,.72):
            box('Palace roof light',(x,3.72,1.15),(.16,.04,.02),'white',.01)
    for side in (-1,1):
        box('Hood shutline',(side*.86,2.28,1.53),(.012,.012,.09),'rubber',.003)

def sleeper(d):
    if not d['sleeper']: return
    start=len(parts)
    rear=d['rear']; roof=d['sleeper']; width=1.17
    aero=d['style']=='aero'
    for side in (-1,1):
        x=side*width
        # Four wall pieces surround a genuine glazed opening, not glass over an
        # opaque wall. The cab passage stays open at the front of the sleeper.
        for y0,y1,z0,z1 in ((1.16,2.56,rear,.12),(3.06,roof-.18,rear,.12),
                            (2.56,3.06,rear,-.68),(2.56,3.06,-.18,.12)):
            if aero and y0==3.06: continue
            panel('Sleeper exterior side '+str(side),[(x,y0,z0),(x,y0,z1),(x,y1,z1),(x,y1,z0)])
            inside=x-side*.055
            panel('Sleeper interior side '+str(side),[(inside,y0+.015,z0),(inside,y1-.015,z0),
                (inside,y1-.015,z1),(inside,y0+.015,z1)],'interior')
        trim=[(x*1.005,1.23,rear+.03),(x*1.005,3.10,rear+.03),(x*1.005,3.10,-.16)] if aero else [(x*1.005,1.23,rear+.03),(x*1.005,roof-.20,rear+.03),(x*1.005,roof-.20,-.16)]
        pipe('Sleeper edge trim '+str(side),trim,.023,'secondary')
        # Window glass and seal on the exterior panel, with a translucent glazed port.
        pipe('Sleeper window surround',[(x,2.57,-.67),(x,3.05,-.67),(x,3.05,-.19),(x,2.57,-.19),(x,2.57,-.67)],.022,'rubber')
        box('Sleeper glazed port',(x*1.023,2.81,-.43),(.012,.37,.37),'glass',.08)
        for y in (1.58,1.91,2.24):
            box('Sleeper side sculpt',(x*1.005,y,-.43),(.015,.014,.73),'secondary',.007)
    back_top=3.06 if aero else roof-.18
    panel('Sleeper rear shell',[(-width,1.16,rear),(width,1.16,rear),(width,back_top,rear),(-width,back_top,rear)],'paint')
    panel('Sleeper rear liner',[(-1.10,1.24,rear+.055),(-1.10,back_top-.04,rear+.055),(1.10,back_top-.04,rear+.055),(1.10,1.24,rear+.055)],'interior')
    if aero: aero_sleeper_roof(d)
    else: loft('Sleeper formed roof',[(rear,1.17,roof-.20,roof),(-.18,1.17,roof-.20,roof),(.12,1.12,d['roof']-.15,d['roof']+.08)],'paint',.06)
    box('Sleeper floor',(0,1.18,(rear+.1)/2),(2.26,.10,.1-rear),'interior',.035)
    box('Berth storage plinth',(0,1.47,rear+.35),(2.16,.48,.64),'wood',.045)
    box('Mattress',(0,1.76,rear+.37),(2.10,.20,.67),'bedding',.095)
    box('Folded duvet',(.27,1.88,rear+.37),(1.46,.055,.64),'fabric',.035)
    pillow=box('Berth pillow',(-.75,1.92,rear+.37),(.47,.16,.48),'bedding',.075)
    pillow.rotation_euler[2]=math.radians(5)
    for x in (-.70,0,.70):
        box('Under-berth drawer',(x,1.46,rear+.702),(.64,.32,.02),'wood',.025)
        box('Drawer pull',(x,1.53,rear+.72),(.20,.025,.03),'chrome',.008)
    for side in (-1,1):
        box('Upper storage locker',(side*.84,3.16,rear+.25),(.51,.49,.40),'wood',.045)
        box('Locker handle',(side*.70,3.13,rear+.46),(.025,.14,.025),'chrome',.007)
        # Curtains frame the passage; no opaque panel blocks cab-to-berth access.
        for fold in range(6):
            cyl('Pleated privacy curtain',(side*(.82+fold*.044),2.50,.08),.034,1.75,'fabric','y',12)
        box('Berth reading light',(side*.70,2.57,rear+.085),(.23,.05,.055),'warm',.02)
    box('Ceiling lamp',(0,roof-.24,-.28),(.45,.04,.24),'warm',.035)
    if d['style']=='luxury':
        box('Luxury upper cabinet',(0,3.37,rear+.23),(1.12,.40,.37),'wood',.05)
        for x in (-.28,.28): box('Upper cabinet pull',(x,3.31,rear+.425),(.16,.02,.025),'chrome',.006)
        box('Interior refrigerator',(1.00,1.70,-.08),(.24,.85,.36),'secondary',.045)
        box('Refrigerator latch',(.855,1.84,.00),(.025,.18,.025),'chrome',.008)
        box('Bedside shelf',(-.95,2.16,-.10),(.26,.055,.33),'wood',.018)
        cyl('Cup recess',(-.94,2.195,-.05),.05,.01,'black')
        for x in (-.72,.72): pipe('Interior accent rail',[(x,roof-.27,rear+.10),(x,roof-.27,.03)],.012,'warm')
    for obj in parts[start:]: obj['TruckTaxiMount']='Cab'

def source_matrix(record,cache):
    if record['id'] in cache: return cache[record['id']]
    if record['parent']==0:
        value=Matrix.Identity(4)
    else:
        pos,rot,scale=record['position'],record['rotation'],record['scale']
        q=Quaternion((rot['w'],rot['x'],rot['z'],-rot['y']))
        value=source_matrix(BYID[record['parent']],cache) @ Matrix.LocRotScale(Vector(p(pos['x'],pos['y'],pos['z'])),q,Vector((scale['x'],scale['z'],scale['y'])))
    cache[record['id']]=value
    return value

def load_source(starter=False,rear_offset=0):
    with bpy.data.libraries.load(str(REFERENCE),link=False) as (source,target): target.objects=source.objects
    cache={}; imported=[]
    for obj in target.objects:
        if obj is None or obj.type!='MESH': continue
        key='Cab' if obj.name=='cabin top' else obj.name
        record=next((r for r in LAYOUT if r['name']==key and r['mesh']),None)
        if record is None: continue
        bpy.context.scene.collection.objects.link(obj)
        obj.animation_data_clear()
        # The actual Unity importer scale is0.8; raw FBX metres alone are not the
        # actual in-game body dimensions. Preserve the vendor file unchanged.
        obj.matrix_world=source_matrix(record,cache) @ Matrix.Scale(.8,4) @ obj.matrix_world
        # Keep complete untouched vendor geometry only in local starter review.
        if not starter and not key.startswith('Whl HD'):
            bpy.data.objects.remove(obj,do_unlink=True); continue
        if key.startswith('Whl HD'):
            # NWH initializes its visual wheels from WheelController contacts;
            # the serialized visual parents themselves are at zero pre-Awake.
            contact=next(r for r in LAYOUT if r['name']==key+'_WheelController')
            pos=contact['position']
            obj.location=Vector(p(pos['x'],pos['y'],pos['z']))
            if key.endswith('RL') or key.endswith('RR'): obj.location.y-=rear_offset
            bpy.context.view_layer.update()
        obj.name='REFERENCE ONLY '+key
        for slot in obj.material_slots:
            n=slot.material.name.lower() if slot.material else ''
            slot.material=M['rubber'] if 'tyre' in n or 'texblack' in n else M['chrome'] if 'rim' in n or 'reflect' in n else M['glass'] if 'window' in n or 'glass' in n else M['paint'] if 'bodycolor' in n else M['black']
        imported.append(obj)
    return imported

def clean_mesh(obj):
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-5)
    bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='BEAUTY',ngon_method='BEAUTY')
    tiny=[f for f in bm.faces if f.calc_area()<1e-10]
    if tiny: bmesh.ops.delete(bm,geom=tiny,context='FACES_ONLY')
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(obj.data); bm.free(); obj.data.update()

def uv_and_export(name):
    # Bake only authored modifiers; the clean source reference is never altered/exported.
    for obj in list(parts):
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        if obj.type!='MESH': bpy.ops.object.convert(target='MESH')
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        clean_mesh(obj)
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.mesh.normals_make_consistent(inside=False)
        bpy.ops.uv.smart_project(angle_limit=1.1519,island_margin=.02)
        bpy.ops.object.mode_set(mode='OBJECT')
    # Export batched copies, retaining individually named editable source parts.
    # Separate wheel/steering groups support existing NWH animation bindings.
    groups={}
    for obj in parts:
        group=obj.name.split('_rim')[0].split('_hub')[0].split('_vent')[0] if obj.name.startswith('Wheel_') else 'SteeringWheel' if obj.name.startswith('Steering wheel') else 'Interior' if any(obj.name.startswith(s) for s in ('Seat ','Interior ','Instrument ','Gauge ','Central display','Air vent','Sleeper interior','Sleeper rear liner','Sleeper floor','Berth ','Mattress','Folded duvet','Upper storage','Locker ','Pleated privacy','Ceiling lamp','Under-berth','Drawer ','Luxury upper','Upper cabinet','Refrigerator','Bedside','Cup recess')) else 'Exterior'
        if group=='Exterior': group='CabExterior' if obj.get('TruckTaxiMount')=='Cab' else 'Chassis'
        if group=='Interior': group='CabInterior'
        if obj.get('TruckTaxiBinding'): group=obj['TruckTaxiBinding']
        if any(obj.name.startswith(s) for s in ('Round headlamp','LED headlamp','Bumper foglamp')): group='LampHeadlamp'
        if any(obj.name.startswith(s) for s in ('Roof rally lamp','Palace roof light')): group='LampHighBeam'
        if any(obj.name.startswith(s) for s in ('Roof amber marker','Side amber marker','Palace side jewel')): group='LampMarker'
        if any(obj.name.startswith(s) for s in ('Direction repeater','Rear turn lamp')): group='LampTurnLeft' if obj.location.x>0 else 'LampTurnRight'
        if obj.name.startswith('Rear stop lamp'): group='LampBrake'
        material=obj.data.materials[0].name
        groups.setdefault((group,material),[]).append(obj)
    exported=[]
    for (group,material),objects in groups.items():
        copies=[]
        for original in objects:
            copy=original.copy(); copy.data=original.data.copy()
            bpy.context.scene.collection.objects.link(copy); copies.append(copy)
        bpy.ops.object.select_all(action='DESELECT')
        for obj in copies: obj.select_set(True)
        bpy.context.view_layer.objects.active=copies[0]
        bpy.ops.object.join()
        combined=bpy.context.object; combined.name=group+'__'+material
        clean_mesh(combined)
        exported.append(combined)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in exported: obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(EXPORTS/(name+'.fbx')),use_selection=True,
        object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
        bake_space_transform=True,use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
    lods=[]
    for lod,ratio in ((1,.45),(2,.16)):
        copies=[]
        for original in exported:
            if original.name.startswith(('CabInterior__','SteeringWheel__','GaugeSpeed__','GaugeRpm__')): continue
            copy=original.copy(); copy.data=original.data.copy()
            bpy.context.scene.collection.objects.link(copy); copies.append(copy)
            if len(copy.data.polygons)>60:
                decimate=copy.modifiers.new('Distance LOD simplification','DECIMATE'); decimate.ratio=ratio
                bpy.context.view_layer.objects.active=copy
                bpy.ops.object.modifier_apply(modifier=decimate.name)
            clean_mesh(copy)
        bpy.ops.object.select_all(action='DESELECT')
        for obj in copies: obj.select_set(True)
        path=EXPORTS/(name+'_LOD'+str(lod)+'.fbx')
        bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},
            axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,
            use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
        lods.append(dict(level=lod,path=str(path),triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in copies)))
        for obj in copies: bpy.data.objects.remove(obj,do_unlink=True)
    for obj in exported: bpy.data.objects.remove(obj,do_unlink=True)
    return sum(sum(len(p.vertices)-2 for p in obj.data.polygons) for obj in parts if obj.type=='MESH'),lods,len(groups)

def review_scene(name,d,source):
    scene=bpy.context.scene
    scene.render.engine='BLENDER_EEVEE'; scene.eevee.use_gtao=True; scene.eevee.gtao_distance=3
    scene.eevee.use_soft_shadows=True; scene.eevee.taa_render_samples=64
    scene.world.color=(.18,.18,.18)
    scene.view_settings.view_transform='Filmic'; scene.view_settings.look='Medium High Contrast'
    scene.view_settings.exposure=-.1
    scene.render.resolution_x=1400; scene.render.resolution_y=900; scene.render.resolution_percentage=100
    bpy.ops.mesh.primitive_plane_add(size=200)
    floor=bpy.context.object; floor.location.z=-.01; floor.name='REVIEW studio floor'
    floor.data.materials.append(mat('REVIEW floor',(.12,.14,.16,1),0,.8))
    target=Vector(p(0,1.8,.1))
    for delta,energy,size in [((5,-3,8),1250,6),((-5,-1,5),950,6),((1,7,7),1600,5)]:
        bpy.ops.object.light_add(type='AREA',location=target+Vector(delta))
        light=bpy.context.object; light.name='REVIEW studio softbox'; light.data.energy=energy; light.data.size=size
        light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
    if d['sleeper']:
        bpy.ops.object.light_add(type='AREA',location=p(0,d['sleeper']-.26,-.25))
        light=bpy.context.object; light.name='REVIEW authored sleeper ceiling light'
        light.data.energy=22; light.data.color=(1,.82,.63); light.data.size=.45
        light.rotation_euler=(Vector(p(0,1.7,-.4))-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add()
    camera=bpy.context.object; scene.camera=camera
    views=[('FrontThreeQuarter',p(8,5.4,9),p(0,1.8,.4),True),
           ('Side',p(10,2.5,0),p(0,1.9,.2),True),
           ('RearThreeQuarter',p(-7,4.8,-8),p(0,1.8,0),True),
           ('Cockpit',p(-.56,2.32,.62),p(-.3,2.15,9),False)]
    if name=='Starter':
        views[-1]=('Cockpit',p(-.578,2.246,1.252),p(-.578,2.246,10),False)
    if d['sleeper']:
        views.append(('SleeperInterior',p(-.43,2.52,.75),p(0,2.25,d['rear']+.23),False))
    for view,eye,look,ortho in views:
        camera.location=eye; camera.rotation_euler=(Vector(look)-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.type='ORTHO' if ortho else 'PERSP'; camera.data.ortho_scale=9.5; camera.data.lens=18 if view=='Cockpit' else 22
        camera.data.clip_start=.015
        scene.render.filepath=str(RENDERS/(name+'_'+view+'.png'))
        bpy.ops.render.render(write_still=True)

def build(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.world=bpy.data.worlds.new('Studio world')
    parts.clear()
    d=DESIGNS[name]
    materials(d); chassis(d); exterior(d); sleeper(d)
    triangles,lods,batches=uv_and_export(name)
    # Sources remain editable, with descriptive part names and material channels.
    reference=load_source(rear_offset=d.get('rearAxleOffset',0))
    for obj in reference: obj.hide_select=True
    bpy.context.scene['TruckTaxi_Model']=name
    bpy.context.scene['Unity_Metres']=True
    bpy.context.scene['OriginalBodyTriangles']=triangles
    bpy.ops.wm.save_as_mainfile(filepath=str(MODELS/(name+'.blend')))
    review_scene(name,d,reference)
    record=dict(model=name,originalBodyTriangles=triangles,originalParts=len(parts),
        exportMeshBatches=batches,lods=lods,
        cabRearZ=d['rear'],
        rearAxleOffset=d.get('rearAxleOffset',0),fifthWheelOffset=d.get('fifthWheelOffset',0),
        sourceReference='Unmodified NWH chassis/wheels: LOCAL REVIEW ONLY, NOT in body FBX',
        sleeper=bool(d['sleeper']),export=str(EXPORTS/(name+'.fbx')),blend=str(MODELS/(name+'.blend')))
    (MODELS/(name+'.json')).write_text(json.dumps(record,indent=2))
    print('MODEL_REVIEW_RENDERED '+json.dumps(record))

def review_starter():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.world=bpy.data.worlds.new('Source review world')
    parts.clear()
    d=dict(DESIGNS['HighwayKing']); d.update(color=(.72,.74,.75,1),sleeper=0)
    materials(d)
    reference=load_source(starter=True)
    bpy.context.scene['ReviewNote']='Source geometry assembly with neutral review materials; not a Unity appearance or gameplay validation.'
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SourceReference/Starter_Assembled_Review.blend'))
    review_scene('Starter',d,reference)
    print('STARTER_SOURCE_ASSEMBLY_REVIEW_RENDERED')

args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else list(DESIGNS)
for model in args:
    if model=='Starter': review_starter()
    else: build(model)
