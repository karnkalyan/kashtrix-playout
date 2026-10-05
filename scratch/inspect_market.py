import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('scratch/market_kcg.json', 'r', encoding='utf-8') as f:
    project = json.load(f)

print(f"Project: {project.get('Name')}")
for idx, l in enumerate(project.get('Layers', [])):
    print(f"[{idx}] {l.get('Name')}: Type={l.get('Type')} Field={l.get('DataField')} Text={repr(l.get('Text', '')[:70])}")
