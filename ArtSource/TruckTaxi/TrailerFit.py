"""Planned geometric fit from ACTUAL serialized trailer collider/kingpin data.
This does not execute NWH coupling and is never reported as a physics test.
"""
import json
import math
from pathlib import Path

root=Path(__file__).resolve().parent
trailer=json.loads((root/'SourceReference/TrailerPrefabLayout.json').read_text())
body=next(r for r in trailer if r['name']=='trailer')
attachment=next(r for r in trailer if r['name']=='AttachmentPoint')
collider=next(c['data'] for c in body['colliders'] if c['type']=='BoxCollider')
center,size=collider['m_Center'],collider['m_Size']
front=body['position']['z']+center['z']+size['z']*.5-attachment['position']['z']
halfwidth=max(abs(center['x']-size['x']*.5),abs(center['x']+size['x']*.5))
radius=math.hypot(halfwidth,front)
results=[]
for path in sorted((root/'Models').glob('*.json')):
    d=json.loads(path.read_text())
    hitch=-1.162+d['fifthWheelOffset']
    axle=-1.62+d['rearAxleOffset']
    clearance=d['cabRearZ']-(hitch+radius)
    errors=[]
    if clearance<.15: errors.append('Insufficient trailer-front corner swept clearance')
    if hitch-axle<.10: errors.append('Fifth wheel must remain ahead of the rear axle')
    results.append(dict(model=d['model'],hitchZ=hitch,rearAxleZ=axle,
        cabRearZ=d['cabRearZ'],minimumPlanarCornerClearance=clearance,
        fifthWheelAheadOfAxle=hitch-axle,errors=errors,
        status='PASS geometry only' if not errors else 'FAIL geometry',
        actualNwhCoupling='NOT RUN'))
report=dict(source='Unmodified NWH SemiTrailer Variant serialized BoxCollider and AttachmentPoint',
    trailerBodyFrontRelativeToKingpin=front,trailerFrontHalfWidth=halfwidth,
    trailerFrontCornerSweptRadius=radius,models=results)
(root/'TrailerFit.json').write_text(json.dumps(report,indent=2))
for row in results: print(row['model'],row['status'],f"corner clearance {row['minimumPlanarCornerClearance']:.3f}m")
if len(results)!=5 or any(row['errors'] for row in results): raise RuntimeError('Planned trailer fit failed')
