import sqlite3, json, os

db_path = os.path.expandvars(r'%LOCALAPPDATA%\KashtrixPlayout\kashtrix.db')
template_path = 'demos/Market · Live Stock & Indices Ticker.kcg'

with open(template_path, 'r', encoding='utf-8') as f:
    fresh_market = json.load(f)

if os.path.exists(db_path):
    conn = sqlite3.connect(db_path)
    cur = conn.cursor()
    cur.execute("SELECT json FROM state WHERE key='cg-projects'")
    row = cur.fetchone()
    if row:
        projects = json.loads(row[0])
        updated = False
        for i, p in enumerate(projects):
            name = p.get('Name', '')
            if 'Market' in name:
                fresh_market['Id'] = p.get('Id', fresh_market['Id'])
                fresh_market['OnAir'] = p.get('OnAir', False)
                projects[i] = fresh_market
                updated = True
                print(f"Updated project {name} in DB.")
        if not updated:
            projects.append(fresh_market)
            print("Appended fresh Market project to DB.")
        
        cur.execute("UPDATE state SET json = ? WHERE key = 'cg-projects'", (json.dumps(projects),))
        conn.commit()
        print("Successfully committed updated cg-projects to kashtrix.db!")
    conn.close()
else:
    print("Database file not found at", db_path)
