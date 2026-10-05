import json
import uuid

with open('scratch/market_kcg.json', 'r', encoding='utf-8') as f:
    project = json.load(f)

ds_id = "74932e11-c231-43f1-b876-70389e65a89b"

# Update Market Live Ticker layer
ticker_layer = next((l for l in project['Layers'] if l.get('Name') == 'Market Live Ticker'), None)
if ticker_layer:
    ticker_layer['DataSourceId'] = ds_id
    ticker_layer['Text'] = (
        'NEPSE    2,684.50    <kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+34.20 (+1.29%)</kcg>    •    '
        'FLOAT    472.10    <kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+5.12 (+1.10%)</kcg>    •    '
        'BANK    1,420.30    <kcg img="market-down.png" fallback="▼"/>    <kcg color="#FF1744">-8.40 (-0.59%)</kcg>    •    '
        'HYDRO    3,110.80    <kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+52.60 (+1.72%)</kcg>    •    '
        'MICRO    4,890.15    <kcg img="market-down.png" fallback="▼"/>    <kcg color="#FF1744">-18.20 (-0.37%)</kcg>    •    '
        'INSUR    11,250.00    <kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+112.50 (+1.01%)</kcg>    •    '
        'HOTEL    5,410.25    <kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+78.90 (+1.48%)</kcg>    •    '
        'MANUF    6,780.40    <kcg img="market-down.png" fallback="▼"/>    <kcg color="#FF1744">-32.10 (-0.47%)</kcg>'
    )

# Rebuild layers preserving the lower third bar and card plate
base_layers = [l for l in project['Layers'] if l.get('Name') in [
    'Market Lower Third Bar', 'Market Header Badge', 'Market Badge Text', 'Market SubBadge Text', 'Market Live Ticker'
]]

card_plate = next((l for l in project['Layers'] if l.get('Name') == 'Market Card Plate'), None)
if card_plate:
    card_plate['X'] = 1360
    card_plate['Y'] = 220
    card_plate['Width'] = 500
    card_plate['Height'] = 490

card_header = next((l for l in project['Layers'] if l.get('Name') == 'Market Card Header'), None)
if card_header:
    card_header['X'] = 1360
    card_header['Y'] = 228
    card_header['Width'] = 500
    card_header['Height'] = 40
    card_header['Text'] = 'TOP MOVERS · NEPSE'

turnover_text = next((l for l in project['Layers'] if l.get('Name') == 'Market Turnover Text'), None)
if turnover_text:
    turnover_text['X'] = 1360
    turnover_text['Y'] = 665
    turnover_text['Width'] = 500
    turnover_text['Height'] = 38
    turnover_text['FontSize'] = 18

def make_text_layer(name, data_field, y, height, font_size, default_text, duration_sec=3.5, color='#00E676'):
    return {
        "Id": str(uuid.uuid4()),
        "Name": name,
        "Type": "Text",
        "Text": default_text,
        "FontFamily": "Segoe UI",
        "FontSize": font_size,
        "Bold": True,
        "Fill": color,
        "HorizontalTextAlignment": "Left",
        "VerticalTextAlignment": "Center",
        "X": 1390,
        "Y": y,
        "Width": 440,
        "Height": height,
        "DataSourceId": ds_id,
        "DataField": data_field,
        "DataItemDurationSeconds": duration_sec,
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

# Minimum 5 rotating top movers layers
rotating_mover = make_text_layer(
    name="Market Rotating Top Mover",
    data_field="MOVER",
    y=275,
    height=65,
    font_size=21,
    default_text='HYDROPOWER  3,110.80\n<kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+52.60 (+1.72%)</kcg>',
    duration_sec=3.5,
    color="#00E5FF"
)

mover1 = make_text_layer(
    name="Market Mover 1",
    data_field="TOP1",
    y=348,
    height=55,
    font_size=18,
    default_text='HYDROPOWER  3,110.80\n<kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+52.60 (+1.72%)</kcg>',
    color="#00E676"
)

mover2 = make_text_layer(
    name="Market Mover 2",
    data_field="TOP2",
    y=410,
    height=55,
    font_size=18,
    default_text='HOTELS & TOURISM  5,410.25\n<kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+78.90 (+1.48%)</kcg>',
    color="#00E676"
)

mover3 = make_text_layer(
    name="Market Mover 3",
    data_field="TOP3",
    y=472,
    height=55,
    font_size=18,
    default_text='NEPSE INDEX  2,684.50\n<kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+34.20 (+1.29%)</kcg>',
    color="#00E676"
)

mover4 = make_text_layer(
    name="Market Mover 4",
    data_field="TOP4",
    y=534,
    height=55,
    font_size=18,
    default_text='NON-LIFE INSURANCE  11,250.00\n<kcg img="market-up.png" fallback="▲"/>    <kcg color="#00E676">+112.50 (+1.01%)</kcg>',
    color="#00E676"
)

mover5 = make_text_layer(
    name="Market Mover 5",
    data_field="TOP5",
    y=596,
    height=55,
    font_size=18,
    default_text='COMMERCIAL BANKS  1,420.30\n<kcg img="market-down.png" fallback="▼"/>    <kcg color="#FF1744">-8.40 (-0.59%)</kcg>',
    color="#FF1744"
)

new_layers = base_layers + [card_plate, card_header, rotating_mover, mover1, mover2, mover3, mover4, mover5, turnover_text]
project['Layers'] = new_layers

with open('scratch/market_kcg.json', 'w', encoding='utf-8') as f:
    json.dump(project, f, indent=2, ensure_ascii=False)

print(f"Updated scratch/market_kcg.json successfully with {len(new_layers)} layers.")
