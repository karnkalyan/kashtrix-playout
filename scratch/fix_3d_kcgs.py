import zlib
import json
import glob
import os

salt = bytes([
    0x5F, 0xA3, 0x19, 0x8C, 0x7E, 0x44, 0xB2, 0x90,
    0x33, 0xD1, 0x6A, 0xF5, 0x82, 0x1B, 0x4D, 0x99,
    0xC4, 0x2A, 0x71, 0xE8, 0x0F, 0x58, 0xB6, 0x3D,
    0x9E, 0x12, 0x85, 0x6F, 0x27, 0xDC, 0x4B, 0x78
])

magic = bytes([0x4B, 0x43, 0x47, 0x31, 0x01])

def encode_kcg(json_str):
    plain_bytes = json_str.encode('utf-8')
    checksum = 0
    for b in plain_bytes:
        checksum = (checksum * 31 + b) & 0x7FFFFFFF
    check_bytes = checksum.to_bytes(4, byteorder='little')
    compressor = zlib.compressobj(zlib.Z_BEST_COMPRESSION, zlib.DEFLATED, -zlib.MAX_WBITS)
    compressed = compressor.compress(plain_bytes) + compressor.flush()
    encrypted = bytearray(len(compressed))
    state = 0x5A
    for i, b in enumerate(compressed):
        key = salt[i % len(salt)]
        cipher = (b ^ key ^ state ^ ((i * 37) & 0xFF)) & 0xFF
        state = (state + cipher + 17) & 0xFF
        encrypted[i] = cipher
    return magic + check_bytes + encrypted

def decode_kcg(raw):
    offset = 9
    cipher = raw[offset:]
    decrypted = bytearray(len(cipher))
    state = 0x5A
    for i, c in enumerate(cipher):
        key = salt[i % len(salt)]
        plain = (c ^ key ^ state ^ ((i * 37) & 0xFF)) & 0xFF
        state = (state + c + 17) & 0xFF
        decrypted[i] = plain
    return zlib.decompress(decrypted, -zlib.MAX_WBITS).decode('utf-8')

patterns = ['cg-demo/3D*.kcg', 'demos/3D*.kcg', 'cgdemo-templates/market/3D*.kcg']
for pat in patterns:
    for fpath in glob.glob(pat):
        with open(fpath, 'rb') as f:
            raw = f.read()
        json_str = decode_kcg(raw)
        data = json.loads(json_str)
        is_flip = 'Flip' in os.path.basename(fpath)
        is_box = 'Box' in os.path.basename(fpath)
        
        target_w = 600.0 if is_flip else 500.0
        target_h = 400.0
        target_x = 660.0 if is_flip else 710.0
        target_y = 340.0
        
        for layer in data.get('Layers', []):
            layer['X'] = target_x
            layer['Y'] = target_y
            layer['Width'] = target_w
            layer['Height'] = target_h
            layer['AnchorX'] = 0.5
            layer['AnchorY'] = 0.5
            layer['Perspective'] = 1100.0
            
            for kf in layer.get('Keyframes', []):
                kf['X'] = target_x
                kf['Y'] = target_y
                kf['Width'] = target_w
                kf['Height'] = target_h
                kf['AnchorX'] = 0.5
                kf['AnchorY'] = 0.5
                kf['ScaleX'] = 1.0
                kf['ScaleY'] = 1.0
                kf['ScaleZ'] = 1.0
                if kf.get('Ease') is None or kf.get('Ease') == '':
                    kf['Ease'] = 'power2.inOut'
        
        new_json = json.dumps(data, separators=(',', ':'))
        new_kcg = encode_kcg(new_json)
        with open(fpath, 'wb') as f:
            f.write(new_kcg)
        print(f"Updated {fpath} successfully.")
