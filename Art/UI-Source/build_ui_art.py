"""
Builds the 2D UI art for the menus.

  python Art/UI-Source/build_ui_art.py

1. Writes editable SVG sources into Art/UI-Source/svg/
2. Rasterises each one (transparent PNG, 2x) with headless Edge into
   Assets/_Project/Art/UI/<group>/  — these are the files you use in Unity.
Edit an SVG (Figma / Illustrator / text editor) and re-run to update the PNGs.
"""
import os, subprocess, pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
SRC = ROOT / "Art" / "UI-Source" / "svg"
OUT = ROOT / "Assets" / "_Project" / "Art" / "UI"
EDGE = next(p for p in [
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe"] if os.path.exists(p))

TEAL, TEAL_D, TEAL_L = "#46E0D2", "#17B3A5", "#9BF5EC"
DARK, DARK_2, LINE = "#1A1F26", "#11151A", "#343C47"

def chamfer(w, h, c, inset=0):
    """Top-left and bottom-right corners cut (the reference's angled tiles)."""
    i = inset
    return f"M{c+i} {i} H{w-i} V{h-c-i} L{w-c-i} {h-i} H{i} V{c+i} Z"

def dots(x, y, color, n=3):
    return "".join(f'<circle cx="{x+k*7}" cy="{y}" r="2.2" fill="{color}"/>' for k in range(n))

def button(fill_a, fill_b, stroke, bevel, dot, w=128, h=128, c=24):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">
  <defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="{fill_a}"/><stop offset="1" stop-color="{fill_b}"/></linearGradient></defs>
  <path d="{chamfer(w, h, c, 1.5)}" fill="url(#g)" stroke="{stroke}" stroke-width="3"/>
  <path d="{chamfer(w, h, c - 3, 8)}" fill="none" stroke="{bevel}" stroke-width="1.5"/>
  {dots(w - 30, 13, dot)}
</svg>'''

def round_btn(fill_a, fill_b, ring):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128">
  <defs><radialGradient id="g" cx="0.38" cy="0.32" r="0.8">
    <stop offset="0" stop-color="{fill_a}"/><stop offset="1" stop-color="{fill_b}"/></radialGradient></defs>
  <circle cx="64" cy="64" r="61" fill="#07090C"/>
  <circle cx="64" cy="64" r="56" fill="url(#g)" stroke="{ring}" stroke-width="3"/>
  <circle cx="64" cy="64" r="47" fill="none" stroke="#FFFFFF" stroke-opacity="0.07" stroke-width="2"/>
</svg>'''

def pill(fill, stroke):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="96" height="64" viewBox="0 0 96 64">
  <rect x="1.5" y="1.5" width="93" height="61" rx="30.5" fill="{fill}" stroke="{stroke}" stroke-width="3"/>
</svg>'''

def panel(fill, stroke, accent=None, c=32):
    stripe = f'<path d="M{c+10} 6 H{c+70}" stroke="{accent}" stroke-width="4" stroke-linecap="round"/>' if accent else ""
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="192" height="192" viewBox="0 0 192 192">
  <path d="{chamfer(192, 192, c, 1.5)}" fill="{fill}" stroke="{stroke}" stroke-width="3"/>
  {stripe}
</svg>'''

def badge():
    return '''<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 64 64">
  <circle cx="32" cy="32" r="29" fill="#0D1014"/><circle cx="32" cy="32" r="24" fill="#FF4D5E"/>
  <rect x="29" y="16" width="6" height="20" rx="3" fill="#fff"/><circle cx="32" cy="45" r="3.6" fill="#fff"/>
</svg>'''

def health_bar():
    stripes = "".join(f'<path d="M{x} 30 L{x+14} 2" stroke="#000" stroke-opacity="0.18" stroke-width="6"/>' for x in range(-10, 330, 18))
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="320" height="32" viewBox="0 0 320 32">
  <defs><linearGradient id="g" x1="0" x2="1"><stop offset="0" stop-color="#FF4D5E"/>
    <stop offset="0.45" stop-color="#FFB020"/><stop offset="1" stop-color="#4ADE80"/></linearGradient>
    <clipPath id="c"><rect x="2" y="2" width="316" height="28" rx="14"/></clipPath></defs>
  <rect x="2" y="2" width="316" height="28" rx="14" fill="url(#g)"/>
  <g clip-path="url(#c)">{stripes}</g>
  <rect x="2" y="2" width="316" height="28" rx="14" fill="none" stroke="#0D1014" stroke-width="3"/>
</svg>'''

def nav_tile(active):
    """Bottom nav tile from the reference: chamfered, two corner dots, active = white + underline."""
    if active:
        body = '''<path d="M24 1.5 H190.5 V206 L174 222.5 H1.5 V24 Z" fill="url(#g)" stroke="#FFFFFF" stroke-width="3"/>
  <rect x="78" y="204" width="60" height="7" rx="3.5" fill="#9AA4B1"/>'''
        grad = '<stop offset="0" stop-color="#FFFFFF"/><stop offset="1" stop-color="#D9DEE3"/>'
        dot = "#9AA4B1"
    else:
        body = '''<path d="M24 1.5 H190.5 V206 L174 222.5 H1.5 V24 Z" fill="url(#g)" stroke="#2E353F" stroke-width="3"/>
  <path d="M27 8 H184 V203 L171 216 H8 V27 Z" fill="none" stroke="#FFFFFF" stroke-opacity="0.05" stroke-width="2"/>'''
        grad = '<stop offset="0" stop-color="#252B33"/><stop offset="1" stop-color="#15191E"/>'
        dot = "#4A525D"
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="192" height="224" viewBox="0 0 192 224">
  <defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1">{grad}</linearGradient></defs>
  {body}
  <circle cx="160" cy="16" r="3.5" fill="{dot}"/><circle cx="172" cy="16" r="3.5" fill="{dot}"/><circle cx="172" cy="28" r="3.5" fill="{dot}"/>
</svg>'''

def quick_card():
    """Grey metallic card (reference bottom-right), big chamfer top-left."""
    return '''<svg xmlns="http://www.w3.org/2000/svg" width="384" height="256" viewBox="0 0 384 256">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#C9CFD5"/><stop offset="0.5" stop-color="#9BA3AC"/><stop offset="1" stop-color="#6F7883"/></linearGradient>
    <linearGradient id="inner" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#3A424C"/><stop offset="1" stop-color="#1E242B"/></linearGradient>
  </defs>
  <path d="M48 2 H382 V254 H2 V48 Z" fill="url(#g)" stroke="#E9EDF1" stroke-width="3"/>
  <path d="M52 12 H372 V244 H12 V52 Z" fill="url(#inner)"/>
  <path d="M52 12 H372" stroke="#FFFFFF" stroke-opacity="0.25" stroke-width="2"/>
  <circle cx="352" cy="28" r="3.5" fill="#5F6976"/><circle cx="364" cy="28" r="3.5" fill="#5F6976"/>
</svg>'''

def drone_silhouette():
    """Filled drone icon used on the Quick Start card (like the reference)."""
    return '''<svg xmlns="http://www.w3.org/2000/svg" width="256" height="160" viewBox="0 0 256 160">
  <g fill="#0D1014">
    <rect x="18" y="30" width="84" height="8" rx="4"/><rect x="154" y="30" width="84" height="8" rx="4"/>
    <rect x="54" y="34" width="12" height="22" rx="3"/><rect x="190" y="34" width="12" height="22" rx="3"/>
    <path d="M60 52 L100 70 L100 78 L56 62 Z"/><path d="M196 52 L156 70 L156 78 L200 62 Z"/>
    <path d="M96 62 H160 Q172 62 172 74 V92 Q172 104 160 104 H96 Q84 104 84 92 V74 Q84 62 96 62 Z"/>
    <path d="M90 104 L72 132 H80 L98 108 Z"/><path d="M166 104 L184 132 H176 L158 108 Z"/>
    <rect x="66" y="130" width="22" height="6" rx="3"/><rect x="168" y="130" width="22" height="6" rx="3"/>
    <rect x="112" y="104" width="32" height="10" rx="3"/>
    <rect x="108" y="112" width="40" height="30" rx="8"/>
  </g>
  <circle cx="128" cy="127" r="9" fill="#46E0D2"/><circle cx="128" cy="127" r="4" fill="#0D1014"/>
</svg>'''

def rail_round():
    """Left-rail button from the reference: dark disc, grey ring, darker outer ring."""
    return '''<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128">
  <defs><radialGradient id="g" cx="0.4" cy="0.35" r="0.8"><stop offset="0" stop-color="#2B3138"/><stop offset="1" stop-color="#15191D"/></radialGradient>
    <linearGradient id="ring" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#8E959D"/><stop offset="1" stop-color="#4A5058"/></linearGradient></defs>
  <circle cx="64" cy="64" r="62" fill="#0B0D10"/>
  <circle cx="64" cy="64" r="56" fill="none" stroke="url(#ring)" stroke-width="7"/>
  <circle cx="64" cy="64" r="51" fill="url(#g)"/>
  <circle cx="64" cy="64" r="51" fill="none" stroke="#000" stroke-opacity="0.5" stroke-width="2"/>
</svg>'''

def arrow_btn():
    return '''<svg xmlns="http://www.w3.org/2000/svg" width="112" height="128" viewBox="0 0 112 128">
  <defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#2A3038"/><stop offset="1" stop-color="#1A1E24"/></linearGradient></defs>
  <rect x="2" y="2" width="108" height="124" rx="12" fill="url(#g)" stroke="#353C45" stroke-width="2"/>
</svg>'''

def bottom_bar():
    """Full-width dark strip behind the nav tiles, with fine diagonal texture and a lit top edge."""
    lines = "".join(f'<path d="M{x} 160 L{x+40} 0" stroke="#FFFFFF" stroke-opacity="0.025" stroke-width="3"/>' for x in range(-40, 260, 8))
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="256" height="160" viewBox="0 0 256 160">
  <defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#22272D"/><stop offset="1" stop-color="#121519"/></linearGradient></defs>
  <rect width="256" height="160" fill="url(#g)"/>{lines}
  <rect width="256" height="3" fill="#3A414A"/>
</svg>'''

def start_btn():
    """Simple START button in the same grey-metal, chamfered vibe as the reference card."""
    return '''<svg xmlns="http://www.w3.org/2000/svg" width="384" height="160" viewBox="0 0 384 160">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#D3D8DD"/><stop offset="0.5" stop-color="#A3ABB4"/><stop offset="1" stop-color="#737C87"/></linearGradient>
    <linearGradient id="inner" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#3B434D"/><stop offset="1" stop-color="#1D232A"/></linearGradient>
  </defs>
  <path d="M40 2 H382 V120 L344 158 H2 V40 Z" fill="url(#g)" stroke="#EEF1F4" stroke-width="3"/>
  <path d="M44 12 H372 V116 L340 148 H12 V44 Z" fill="url(#inner)"/>
  <path d="M44 12 H372" stroke="#FFFFFF" stroke-opacity="0.22" stroke-width="2"/>
  <circle cx="350" cy="28" r="3.5" fill="#6B7480"/><circle cx="362" cy="28" r="3.5" fill="#6B7480"/>
</svg>'''

def status_tag():
    """Green outlined slanted tag (reference: 'MODULE 1-4 COMPLETED')."""
    return '''<svg xmlns="http://www.w3.org/2000/svg" width="320" height="96" viewBox="0 0 320 96">
  <path d="M30 4 H316 L290 92 H4 Z" fill="#0E1215" fill-opacity="0.9" stroke="#4ADE80" stroke-width="4" stroke-linejoin="round"/>
</svg>'''

# ---------------------------------------------------------------- 2D drone illustration
def drone():
    def motor(x, y, s=1.0):
        return f'''<g transform="translate({x} {y}) scale({s})">
      <ellipse cx="0" cy="56" rx="34" ry="12" fill="{TEAL}" opacity="0.35" filter="url(#glow)"/>
      <rect x="-26" y="-6" width="52" height="54" rx="10" fill="url(#metal)"/>
      <rect x="-26" y="18" width="52" height="8" fill="#0D1014" opacity="0.55"/>
      <ellipse cx="0" cy="-6" rx="26" ry="9" fill="#DDE2E7"/>
      <ellipse cx="0" cy="-8" rx="12" ry="4.5" fill="#2A3038"/>
      <rect x="-27" y="34" width="54" height="5" rx="2" fill="{TEAL}" opacity="0.9"/>
    </g>'''

    def prop(x, y, rx, ry):
        return f'''<g transform="translate({x} {y})">
      <ellipse rx="{rx}" ry="{ry}" fill="url(#blur)"/>
      <ellipse rx="{rx}" ry="{ry}" fill="none" stroke="#FFFFFF" stroke-opacity="0.18" stroke-width="2"/>
      <path d="M{-rx*0.92} {-ry*0.18} Q 0 {-ry*0.55} {rx*0.92} {ry*0.18}" stroke="#20262E" stroke-opacity="0.45" stroke-width="{ry*0.22}" stroke-linecap="round" fill="none"/>
      <path d="M{-rx*0.7} {ry*0.35} Q 0 {ry*0.05} {rx*0.7} {-ry*0.35}" stroke="#20262E" stroke-opacity="0.25" stroke-width="{ry*0.16}" stroke-linecap="round" fill="none"/>
      <ellipse rx="12" ry="5" fill="#2A3038"/>
    </g>'''

    def arm(x1, y1, x2, y2, w):
        return (f'<path d="M{x1} {y1} L{x2} {y2}" stroke="#7E8792" stroke-width="{w+6}" stroke-linecap="round"/>'
                f'<path d="M{x1} {y1} L{x2} {y2}" stroke="url(#armg)" stroke-width="{w}" stroke-linecap="round"/>'
                f'<path d="M{x1} {y1 - w*0.25} L{x2} {y2 - w*0.25}" stroke="#FFFFFF" stroke-opacity="0.35" stroke-width="{w*0.2}" stroke-linecap="round"/>')

    def leg(x, y, h):
        return (f'<path d="M{x-9} {y} L{x-6} {y+h} L{x+6} {y+h} L{x+9} {y} Z" fill="url(#armg)" stroke="#7E8792" stroke-width="2"/>'
                f'<rect x="{x-9}" y="{y+h-4}" width="18" height="10" rx="4" fill="#20262E"/>')

    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="1000" viewBox="0 0 1600 1000">
  <defs>
    <linearGradient id="shell" x1="0" y1="0" x2="0.3" y2="1">
      <stop offset="0" stop-color="#F7F9FA"/><stop offset="0.55" stop-color="#D3D9DE"/><stop offset="1" stop-color="#9AA3AD"/></linearGradient>
    <linearGradient id="deck" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#FFFFFF"/><stop offset="1" stop-color="#E1E6EA"/></linearGradient>
    <linearGradient id="bat" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#C9D0D7"/><stop offset="1" stop-color="#9EA7B1"/></linearGradient>
    <linearGradient id="armg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#F0F3F5"/><stop offset="1" stop-color="#AEB6BF"/></linearGradient>
    <linearGradient id="metal" x1="0" x2="1">
      <stop offset="0" stop-color="#5D6570"/><stop offset="0.45" stop-color="#C9CFD5"/><stop offset="1" stop-color="#4A515B"/></linearGradient>
    <radialGradient id="blur"><stop offset="0" stop-color="#FFFFFF" stop-opacity="0.05"/>
      <stop offset="0.75" stop-color="#C8D0D8" stop-opacity="0.22"/><stop offset="1" stop-color="#FFFFFF" stop-opacity="0.05"/></radialGradient>
    <radialGradient id="lens" cx="0.4" cy="0.35"><stop offset="0" stop-color="#5D8BFF"/>
      <stop offset="0.35" stop-color="#14203A"/><stop offset="1" stop-color="#05070B"/></radialGradient>
    <radialGradient id="shadow"><stop offset="0" stop-color="#000" stop-opacity="0.55"/><stop offset="1" stop-color="#000" stop-opacity="0"/></radialGradient>
    <filter id="glow" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="9"/></filter>
  </defs>

  <ellipse cx="800" cy="860" rx="560" ry="70" fill="url(#shadow)"/>

  <!-- rear arms, legs and motors (behind the body) -->
  {arm(700, 400, 470, 300, 34)}{arm(900, 400, 1130, 300, 34)}
  {leg(470, 330, 120)}{leg(1130, 330, 120)}
  {motor(470, 300, 0.85)}{motor(1130, 300, 0.85)}

  <!-- body -->
  <path d="M800 230 C905 230 968 285 972 372 L962 560 C956 650 888 712 800 712 C712 712 644 650 638 560 L628 372 C632 285 695 230 800 230 Z"
        fill="url(#shell)" stroke="#7E8792" stroke-width="4"/>
  <path d="M800 252 C885 252 935 296 940 366 L936 470 L664 470 L660 366 C665 296 715 252 800 252 Z" fill="url(#deck)"/>
  <!-- battery bay -->
  <rect x="690" y="270" width="220" height="175" rx="34" fill="url(#bat)" stroke="#7E8792" stroke-width="3"/>
  <g fill="#7E8792" opacity="0.8"><rect x="735" y="300" width="130" height="10" rx="5"/><rect x="735" y="320" width="130" height="10" rx="5"/><rect x="735" y="340" width="130" height="10" rx="5"/></g>
  <g fill="{TEAL}"><circle cx="760" cy="400" r="7"/><circle cx="785" cy="400" r="7"/><circle cx="810" cy="400" r="7"/></g>
  <circle cx="835" cy="400" r="7" fill="#5F6976"/>
  <rect x="760" y="432" width="80" height="16" rx="6" fill="#4A515B"/>
  <!-- vents + power button -->
  <g fill="#3A424E"><rect x="680" y="510" width="62" height="11" rx="5"/><rect x="680" y="530" width="62" height="11" rx="5"/><rect x="680" y="550" width="62" height="11" rx="5"/>
    <rect x="858" y="510" width="62" height="11" rx="5"/><rect x="858" y="530" width="62" height="11" rx="5"/><rect x="858" y="550" width="62" height="11" rx="5"/></g>
  <circle cx="800" cy="520" r="20" fill="#E9EDF1" stroke="#7E8792" stroke-width="4"/>
  <!-- nose LEDs -->
  <ellipse cx="712" cy="640" rx="18" ry="9" fill="#FFF6D8"/><ellipse cx="888" cy="640" rx="18" ry="9" fill="#FFF6D8"/>

  <!-- front arms, legs and motors -->
  {arm(690, 590, 340, 660, 40)}{arm(910, 590, 1260, 660, 40)}
  {leg(340, 690, 150)}{leg(1260, 690, 150)}
  {motor(340, 660)}{motor(1260, 660)}

  <!-- gimbal + camera -->
  <rect x="742" y="690" width="116" height="42" rx="14" fill="#B9C1C9" stroke="#7E8792" stroke-width="3"/>
  <rect x="728" y="720" width="22" height="72" rx="8" fill="#AEB6BF"/><rect x="850" y="720" width="22" height="72" rx="8" fill="#AEB6BF"/>
  <rect x="748" y="722" width="104" height="92" rx="30" fill="url(#shell)" stroke="#7E8792" stroke-width="3"/>
  <circle cx="800" cy="768" r="34" fill="#11151A" stroke="#4A515B" stroke-width="5"/>
  <circle cx="800" cy="768" r="24" fill="url(#lens)"/>
  <circle cx="790" cy="757" r="6" fill="#FFFFFF" opacity="0.7"/>

  <!-- spinning props (on top) -->
  {prop(470, 290, 190, 46)}{prop(1130, 290, 190, 46)}
  {prop(340, 648, 230, 60)}{prop(1260, 648, 230, 60)}
</svg>'''

ASSETS = {
    "Buttons/btn_primary": button(TEAL_L, TEAL_D, TEAL_L, "#FFFFFF66", "#0D1014"),
    "Buttons/btn_ghost": button("#232A33", DARK_2, LINE, "#FFFFFF14", "#5F6976"),
    "Buttons/btn_danger": button("#FF7A87", "#D93445", "#FFA3AC", "#FFFFFF55", "#3A0A10"),
    "Buttons/btn_ok": button("#7CF0A6", "#27B663", "#B4F7CB", "#FFFFFF55", "#062B14"),
    "Buttons/btn_disabled": button("#1D2228", "#171B20", "#2B313B", "#FFFFFF08", "#2B313B"),
    "Buttons/tile": nav_tile(False),
    "Buttons/tile_active": nav_tile(True),
    "Panels/card_quickstart_metal": quick_card(),
    "Buttons/btn_rail": rail_round(),
    "Buttons/btn_arrow": arrow_btn(),
    "Buttons/btn_start": start_btn(),
    "Panels/bottom_bar": bottom_bar(),
    "Panels/status_tag": status_tag(),
    "Icons/drone_silhouette": drone_silhouette(),
    "Buttons/btn_round": round_btn("#2C333C", "#14181D", LINE),
    "Buttons/btn_round_primary": round_btn(TEAL_L, TEAL_D, TEAL_L),
    "Panels/panel": panel("#161A20F2", LINE),
    "Panels/panel_glass": panel("#0D1014B8", "#FFFFFF1F"),
    "Panels/card_quickstart": panel("#13322FF2", TEAL, accent=TEAL, c=40),
    "Panels/pill": pill("#0D1014B8", "#FFFFFF1F"),
    "Panels/chip": pill("#1F242C", LINE),
    "Badges/badge_alert": badge(),
    "Badges/health_bar": health_bar(),
    "Drones/skyfold_f4_hero": drone(),
}

def main():
    for key, svg in ASSETS.items():
        src = SRC / f"{key}.svg"
        src.parent.mkdir(parents=True, exist_ok=True)
        src.write_text(svg, encoding="utf-8")
        w = int(svg.split('width="')[1].split('"')[0]); h = int(svg.split('height="')[1].split('"')[0])
        out = OUT / f"{key}.png"
        out.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars",
                        "--default-background-color=00000000", "--force-device-scale-factor=2",
                        f"--window-size={w},{h}", f"--screenshot={out}", src.as_uri()],
                       check=True, capture_output=True)
        print("ok", out.relative_to(ROOT))

if __name__ == "__main__":
    main()
