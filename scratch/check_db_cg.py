import sqlite3, json, os

db_path = os.path.expandvars(r'%LOCALAPPDATA%\KashtrixPlayout\kashtrix.db')
print("Checking DB at:", db_path)
if os.path.exists(db_path):
    conn = sqlite3.connect(db_path)
    cur = conn.cursor()
    cur.execute("SELECT json FROM state WHERE key='cg-projects'")
    row = cur.fetchone()
    if row:
        projects = json.loads(row[0])
        for p in projects:
            if 'Market' in p.get('Name', ''):
                mover_layers = [l for l in p.get('Layers', []) if 'Mover' in l.get('Name', '') or 'MOVER' in l.get('DataField', '')]
                print(f"Project: {p.get('Name')}, total layers: {len(p.get('Layers', []))}, mover layers: {len(mover_layers)}")
                for ml in mover_layers:
                    print("  ", ml.get('Name'), "Field:", ml.get('DataField'), "Y:", ml.get('Y'), "Anim:", ml.get('TextAnimationPreset'))
    conn.close()
else:
    print("DB does not exist")
