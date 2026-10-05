import json
import zlib
import os

MAGIC_HEADER = bytes([0x4B, 0x43, 0x47, 0x31, 0x01])

KEY_SALT = [
    0x5F, 0xA3, 0x19, 0x8C, 0x7E, 0x44, 0xB2, 0x90,
    0x33, 0xD1, 0x6A, 0xF5, 0x82, 0x1B, 0x4D, 0x99,
    0xC4, 0x2A, 0x71, 0xE8, 0x0F, 0x58, 0xB6, 0x3D,
    0x9E, 0x12, 0x85, 0x6F, 0x27, 0xDC, 0x4B, 0x78
]

def save_composition(file_path, project_dict):
    json_str = json.dumps(project_dict, ensure_ascii=False, separators=(',', ':'))
    plain_bytes = json_str.encode('utf-8')
    
    # Raw deflate compression (wbits=-15 for no zlib header)
    compressor = zlib.compressobj(level=9, method=zlib.DEFLATED, wbits=-15)
    compressed = compressor.compress(plain_bytes) + compressor.flush()
    
    # Rolling cipher encryption
    encrypted = bytearray(len(compressed))
    state = 0x5A
    for i in range(len(compressed)):
        key_byte = KEY_SALT[i % len(KEY_SALT)]
        b = compressed[i]
        cipher = (b ^ key_byte ^ state ^ ((i * 37) & 0xFF)) & 0xFF
        state = (state + cipher + 17) & 0xFF
        encrypted[i] = cipher
        
    # Checksum
    checksum = 0
    for b in plain_bytes:
        checksum = (checksum * 31 + b) & 0x7FFFFFFF
    check_bytes = checksum.to_bytes(4, byteorder='little', signed=False)
    
    with open(file_path, 'wb') as f:
        f.write(MAGIC_HEADER)
        f.write(check_bytes)
        f.write(encrypted)
    print(f"Saved encrypted KCG to {file_path} ({len(plain_bytes)} -> {len(encrypted)} bytes)")

def load_composition(file_path):
    with open(file_path, 'rb') as f:
        raw = f.read()
    
    assert raw[:5] == MAGIC_HEADER, "Invalid magic header"
    check_bytes = raw[5:9]
    encrypted = raw[9:]
    
    decrypted = bytearray(len(encrypted))
    state = 0x5A
    for i in range(len(encrypted)):
        cipher = encrypted[i]
        key_byte = KEY_SALT[i % len(KEY_SALT)]
        plain = (cipher ^ key_byte ^ state ^ ((i * 37) & 0xFF)) & 0xFF
        state = (state + cipher + 17) & 0xFF
        decrypted[i] = plain
        
    decompressor = zlib.decompressobj(wbits=-15)
    decompressed = decompressor.decompress(decrypted) + decompressor.flush()
    project = json.loads(decompressed.decode('utf-8'))
    return project

# Load updated project
with open('scratch/market_kcg.json', 'r', encoding='utf-8') as f:
    project = json.load(f)

# Save to target directories
targets = [
    'cg-demo/Market · Live Stock & Indices Ticker.kcg',
    'demos/Market · Live Stock & Indices Ticker.kcg'
]

for t in targets:
    save_composition(t, project)
    # Verify roundtrip
    verified = load_composition(t)
    assert verified['Name'] == project['Name']
    assert len(verified['Layers']) == len(project['Layers'])
    print(f"Verified roundtrip for {t}: {verified['Name']} with {len(verified['Layers'])} layers.")

print("All KCG market files successfully updated and verified!")
