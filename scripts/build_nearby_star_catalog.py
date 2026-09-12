#!/usr/bin/env python3
import argparse,csv,hashlib,json,math,os
from collections import defaultdict
SOURCE_URL='https://raw.githubusercontent.com/astronexus/HYG-Database/c7f7f883fe678cc7680169a50ccd7dcc49b060ce/hyg/CURRENT/hygdata_v41.csv'
SOURCE_SHA256='d9f69fd86bbf90a4e4d52b4c5c53eacfa6dfc0bfdef85bfd94f095e0bebe4ebd'; LY=3.26156
def t(r,k): return (r.get(k) or '').strip()
def n(r,k):
 try:return float(t(r,k))
 except ValueError:return None
def naming(r):
 for k in ('proper','bf','gl'):
  if t(r,k): return t(r,k),('proper' if k=='proper' else 'catalogue')
 for k in ('hip','hd'):
  if t(r,k): return k.upper()+' '+t(r,k),'catalogue'
 return 'HYG '+t(r,'id'),'catalogue'
def build(source):
 raw=open(source,'rb').read(); sha=hashlib.sha256(raw).hexdigest()
 if sha!=SOURCE_SHA256: raise ValueError('source SHA-256 does not match pinned HYG commit: '+sha)
 valid=[]; byid={}
 for r in csv.DictReader(raw.decode('utf-8-sig').splitlines()):
  try: hid=int(t(r,'id')); pri=int(t(r,'comp_primary'))
  except ValueError: continue
  v=[n(r,k) for k in ('dist','x','y','z')]
  if any(x is None or not math.isfinite(x) for x in v): continue
  d,x,y,z=v
  if d>=100000 or d<0 or (d==0 and hid!=0): continue
  item=(hid,pri,r,d,x,y,z); valid.append(item); byid[hid]=item
 groups=defaultdict(list)
 for item in valid:
  if item[1] in byid: groups[item[1]].append(item)
 systems=[]
 for pri,ms in groups.items():
  p=byid[pri]; _,_,r,d,x,y,z=p; name,kind=naming(r); ordered=[p]+sorted((m for m in ms if m[0]!=pri),key=lambda m:m[0])
  comps=[{'name':naming(m[2])[0],'spectralType':t(m[2],'spect'),'hygId':m[0]} for m in ordered]
  if pri==0: lx=ly=lz=0.0
  else:
   scale=d*LY/math.sqrt(x*x+y*y+z*z); lx,ly,lz=x*scale,y*scale,z*scale
  systems.append({'hygId':pri,'name':name,'nameKind':kind,'distanceParsecs':d,'xLightYears':lx,'yLightYears':ly,'zLightYears':lz,'spectralType':t(r,'spect'),'components':comps})
 systems.sort(key=lambda s:(s['distanceParsecs'],s['hygId'])); systems=systems[:500]; seen=set()
 for s in systems:
  k=s['name'].casefold()
  if k in seen:s['name']+=' [HYG %d]'%s['hygId']
  seen.add(k)
 out={'catalogVersion':'hyg-nearby-500-v1','sourceUrl':SOURCE_URL,'sourceSha256':sha,'epoch':'J2000','systems':systems}; validate(out); return json.dumps(out,ensure_ascii=False,indent=2)+'\n'
def validate(o):
 ss=o['systems']; assert len(ss)==500 and len({s['hygId'] for s in ss})==500 and len({s['name'].casefold() for s in ss})==500
 for s in ss:
  assert all(math.isfinite(s[k]) for k in ('distanceParsecs','xLightYears','yLightYears','zLightYears'))
  q=math.sqrt(sum(s[k]**2 for k in ('xLightYears','yLightYears','zLightYears'))); assert abs(q-s['distanceParsecs']*LY)<.001
  assert s['components'] and s['components'][0]['hygId']==s['hygId']
 assert any(s['hygId']==0 and s['xLightYears']==s['yLightYears']==s['zLightYears']==0 for s in ss)
def main():
 ap=argparse.ArgumentParser(); ap.add_argument('--source',required=True); ap.add_argument('--output',default='data/astronomy/hyg-nearby-500-v1.json'); ap.add_argument('--verify',action='store_true'); a=ap.parse_args(); expected=build(a.source)
 if a.verify:
  if not os.path.exists(a.output) or open(a.output,encoding='utf-8').read()!=expected: raise SystemExit('output differs from reproducible catalog')
  print('verified 500 systems; output unchanged')
 else:
  os.makedirs(os.path.dirname(a.output) or '.',exist_ok=True); open(a.output,'w',encoding='utf-8',newline='\n').write(expected); print('wrote',a.output)
if __name__=='__main__':main()
