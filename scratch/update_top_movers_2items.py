import json
import uuid
import os

with open('scratch/market_kcg.json', 'r', encoding='utf-8') as f:
    project = json.load(f)

ds_id = "74932e11-c231-43f1-b876-70389e65a89b"

# Keep the base layers: lower third bar, header badge, badge text, subbadge text, ticker
base_layers = [l for l in project['Layers'] if l.get('Name') in [
    'Market Lower Third Bar', 'Market Header Badge', 'Market Badge Text', 'Market SubBadge Text', 'Market Live Ticker'
]]

# Update card plate: sleek 320px height card
card_plate = next((l for l in project['Layers'] if l.get('Name') == 'Market Card Plate'), None)
if card_plate is None:
    card_plate = {
        "Id": str(uuid.uuid4()), "Name": "Market Card Plate", "Type": "Shape", "ShapeKind": "Rectangle",
        "Fill": "#00000000", "Background": "#0B1522", "UseGradient": True, "GradientColor2": "#05090F",
        "UseBorder": True, "BorderColor": "#00E5FF", "BorderWidth": 2.0, "CornerRadius": 14.0
    }
card_plate['X'] = 1360
card_plate['Y'] = 220
card_plate['Width'] = 500
card_plate['Height'] = 325

card_header = next((l for l in project['Layers'] if l.get('Name') == 'Market Card Header'), None)
if card_header is None:
    card_header = {
        "Id": str(uuid.uuid4()), "Name": "Market Card Header", "Type": "Text", "FontFamily": "Segoe UI",
        "Bold": True, "HorizontalTextAlignment": "Center", "VerticalTextAlignment": "Center"
    }
card_header['X'] = 1360
card_header['Y'] = 228
card_header['Width'] = 500
card_header['Height'] = 40
card_header['FontSize'] = 20
card_header['Fill'] = "#00E5FF"
card_header['Text'] = 'TOP MOVERS · NEPSE'

def make_slot_layer(name, data_field, y, default_text, default_color='#00E676'):
    return {
        "Id": str(uuid.uuid4()),
        "Name": name,
        "Type": "Text",
        "Text": default_text,
        "FontFamily": "Segoe UI",
        "FontSize": 20,
        "Bold": True,
        "Fill": default_color,
        "HorizontalTextAlignment": "Left",
        "VerticalTextAlignment": "Center",
        "X": 1390,
        "Y": y,
        "Width": 440,
        "Height": 66,
        "DataSourceId": ds_id,
        "DataField": data_field,
        "DataItemDurationSeconds": 4.0,
        "TextAnimationPreset": "Push Up",
        "TextAnimationDurationSeconds": 0.45,
        "StartSeconds": 0,
        "EndSeconds": 20.0,
        "DurationSeconds": 20.0,
        "Visible": True,
        "Opacity": 1.0,
        "ScaleX": 1.0,
        "ScaleY": 1.0,
        "ScaleZ": 1.0,
        "AnchorX": 0.5,
        "AnchorY": 0.5,
        "Perspective": 1200
    }

# 2 items rotating with Push Up / In-Out animation
mover_slot1 = make_slot_layer(
    name="Market Top Mover 1",
    data_field="MOVER_SLOT_1",
    y=282,
    default_text='HOTELS & TOURISM  5,410.25\n<kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+78.90 (+1.48%)</kcg>',
    default_color="#00E676"
)

mover_slot2 = make_slot_layer(
    name="Market Top Mover 2",
    data_field="MOVER_SLOT_2",
    y=362,
    default_text='HYDROPOWER SECTOR  3,110.80\n<kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+52.60 (+1.72%)</kcg>',
    default_color="#00E676"
)

turnover_text = next((l for l in project['Layers'] if l.get('Name') == 'Market Turnover Text'), None)
if turnover_text is None:
    turnover_text = {
        "Id": str(uuid.uuid4()), "Name": "Market Turnover Text", "Type": "Text", "FontFamily": "Segoe UI",
        "Bold": True, "HorizontalTextAlignment": "Center", "VerticalTextAlignment": "Center"
    }
turnover_text['X'] = 1360
turnover_text['Y'] = 485
turnover_text['Width'] = 500
turnover_text['Height'] = 38
turnover_text['FontSize'] = 17
turnover_text['Fill'] = "#FFD600"
turnover_text['Text'] = 'TOTAL TURNOVER: Rs 7.82 Arba'

# Combine base layers + card plate + card header + 2 rotating mover slots + turnover
new_layers = base_layers + [card_plate, card_header, mover_slot1, mover_slot2, turnover_text]
project['Layers'] = new_layers

# Target destinations
target_paths = [
    'scratch/market_kcg.json',
    'cg-demo/Market · Live Stock & Indices Ticker.kcg',
    'demos/Market · Live Stock & Indices Ticker.kcg',
    'src/BroadcastPlayout.App/demos/Market · Live Stock & Indices Ticker.kcg',
    'cgdemo-templates/Market · Live Stock & Indices Ticker.kcg'
]

for p in target_paths:
    if os.path.exists(os.path.dirname(p)):
        with open(p, 'w', encoding='utf-8') as f:
            json.dump(project, f, indent=2, ensure_ascii=False)
        print(f"Updated {p} successfully.")

print("All Market KCG templates updated to 2-item rotating slots with Push Up animation.")
