#!/usr/bin/env python3
"""
make_myriac_font.py
Converts the Myriac Script PNG images into MyriacScript.ttf

Requirements (install once):
    pip install fonttools Pillow numpy opencv-python

Output:
    Myria/MyriacScript.ttf

Ch  → Private Use Area U+E000 / U+E001
Sch → Private Use Area U+E002 / U+E003
"""

from pathlib import Path
import numpy as np
import cv2
from PIL import Image
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib.tables._g_l_y_f import Glyph
from fontTools.ttLib import newTable

# ── Paths ─────────────────────────────────────────────────────────────────────
SCRIPT_DIR = Path(__file__).parent / "Myriac Script"
OUTPUT     = Path(__file__).parent / "MyriacScript.ttf"

# ── Font metrics (in font units, 1000 = 1 em) ─────────────────────────────────
UNITS_PER_EM = 1000
ASCENDER     = 800
DESCENDER    = -200
CAP_HEIGHT   = 720
X_HEIGHT     = 520

# ── Glyph map: filename → (glyph_name, unicode_codepoint) ────────────────────
GLYPHS = {
    "Capital A.png":   ("A",   0x0041),
    "Capital B.png":   ("B",   0x0042),
    "Capital C.png":   ("C",   0x0043),
    "Capital D.png":   ("D",   0x0044),
    "Capital E.png":   ("E",   0x0045),
    "Capital F.png":   ("F",   0x0046),
    "Capital G.png":   ("G",   0x0047),
    "Capital H.png":   ("H",   0x0048),
    "Capital I.png":   ("I",   0x0049),
    "Capital J.png":   ("J",   0x004A),
    "Capital K.png":   ("K",   0x004B),
    "Capital L.png":   ("L",   0x004C),
    "Capital M.png":   ("M",   0x004D),
    "Capital N.png":   ("N",   0x004E),
    "Capital O.png":   ("O",   0x004F),
    "Capital P.png":   ("P",   0x0050),
    "Capital Q.png":   ("Q",   0x0051),
    "Capital R.png":   ("R",   0x0052),
    "Capital S.png":   ("S",   0x0053),
    "Capital T.png":   ("T",   0x0054),
    "Capital U.png":   ("U",   0x0055),
    "Capital V.png":   ("V",   0x0056),
    "Capital W.png":   ("W",   0x0057),
    "Capital X.png":   ("X",   0x0058),
    "Capital Y.png":   ("Y",   0x0059),
    "Capital Z.png":   ("Z",   0x005A),
    "small a.png":     ("a",   0x0061),
    "small b.png":     ("b",   0x0062),
    "small c.png":     ("c",   0x0063),
    "small d.png":     ("d",   0x0064),
    "small e.png":     ("e",   0x0065),
    "small f.png":     ("f",   0x0066),
    "small g.png":     ("g",   0x0067),
    "small h.png":     ("h",   0x0068),
    "small i.png":     ("i",   0x0069),
    "small j.png":     ("j",   0x006A),
    "small k.png":     ("k",   0x006B),
    "small l.png":     ("l",   0x006C),
    "small m.png":     ("m",   0x006D),
    "small n.png":     ("n",   0x006E),
    "small o.png":     ("o",   0x006F),
    "small p.png":     ("p",   0x0070),
    "small q.png":     ("q",   0x0071),
    "small r.png":     ("r",   0x0072),
    "small s.png":     ("s",   0x0073),
    "small t.png":     ("t",   0x0074),
    "small u.png":     ("u",   0x0075),
    "small v.png":     ("v",   0x0076),
    "small w.png":     ("w",   0x0077),
    "small x.png":     ("x",   0x0078),
    "small y.png":     ("y",   0x0079),
    "small z.png":     ("z",   0x007A),
    # Digraphs → Private Use Area
    "Capital Ch.png":  ("Ch",  0xE000),
    "small ch.png":    ("ch",  0xE001),
    "Capital Sch.png": ("Sch", 0xE002),
    "small sch.png":   ("sch", 0xE003),
}


# ── Image → binary ────────────────────────────────────────────────────────────

def load_binary(path: Path) -> np.ndarray:
    """Return a uint8 binary image (255 = ink, 0 = empty). Handles white and
    transparent backgrounds."""
    img = Image.open(path).convert("RGBA")
    arr = np.array(img, dtype=np.int32)
    r, g, b, a = arr[:,:,0], arr[:,:,1], arr[:,:,2], arr[:,:,3]
    ink = (a > 64) & ((r + g + b) < 384)
    return ink.astype(np.uint8) * 255


# ── Contour extraction ────────────────────────────────────────────────────────

def image_to_font_contours(binary: np.ndarray):
    """
    Trace the binary image with OpenCV, scale to font units, flip Y axis.
    Returns (contours, advance_width, lsb).
    """
    h, w = binary.shape
    scale = (ASCENDER - DESCENDER) / h

    def trace(img, reverse: bool):
        contours_cv, _ = cv2.findContours(
            img, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_TC89_KCOS
        )
        result = []
        for cnt in contours_cv:
            arc = cv2.arcLength(cnt, True)
            if arc < 4:
                continue
            epsilon = max(0.3, 0.004 * arc)
            approx = cv2.approxPolyDP(cnt, epsilon, True)
            if len(approx) < 3:
                continue
            pts = [
                (int(round(pt[0][0] * scale)), int(round(ASCENDER - pt[0][1] * scale)))
                for pt in approx
            ]
            result.append(pts[::-1] if reverse else pts)
        return result

    # Outer fill contours — reversed so they are CW (filled) in Y-up
    font_contours = trace(binary, reverse=True)

    # Detect enclosed holes: use morphological closing to seal any small gaps
    # in strokes, then flood-fill from the border. Any black pixel still
    # unreachable is enclosed by ink and should become a hole in the glyph.
    k = max(3, int(min(h, w) * 0.015))
    kernel  = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (k, k))
    closed  = cv2.morphologyEx(binary, cv2.MORPH_CLOSE, kernel)
    flooded = closed.copy()
    cv2.floodFill(flooded, np.zeros((h + 2, w + 2), np.uint8), (0, 0), 255)
    hole_binary = ((flooded == 0) & (binary == 0)).astype(np.uint8) * 255

    if hole_binary.any():
        # Hole contours — not reversed so they are CCW (holes) in Y-up
        font_contours.extend(trace(hole_binary, reverse=False))

    advance_width = int(round(w * scale))
    all_x = [pt[0] for c in font_contours for pt in c]
    lsb   = min(all_x) if all_x else 0

    return font_contours, advance_width, lsb


# ── Glyph assembly ────────────────────────────────────────────────────────────

def contours_to_ttglyph(contours):
    """Draw font contours into a TTGlyphPen and return the TTGlyph."""
    pen = TTGlyphPen(None)
    for contour in contours:
        if len(contour) < 3:
            continue
        pen.moveTo(contour[0])
        for pt in contour[1:]:
            pen.lineTo(pt)
        pen.closePath()
    return pen.glyph()


def make_notdef(width: int):
    """Hollow rectangle .notdef glyph. Returns (glyph, lsb)."""
    pen   = TTGlyphPen(None)
    m     = int(width * 0.08)
    t     = ASCENDER  - m
    bot   = DESCENDER + m
    r     = width     - m
    # Outer rectangle — clockwise in Y-up = filled
    pen.moveTo((m, bot))
    pen.lineTo((m, t))
    pen.lineTo((r, t))
    pen.lineTo((r, bot))
    pen.closePath()
    # Inner cutout — counter-clockwise in Y-up = hole
    inner = max(m, int(width * 0.12))
    pen.moveTo((m + inner, bot + inner))
    pen.lineTo((r - inner, bot + inner))
    pen.lineTo((r - inner, t   - inner))
    pen.lineTo((m + inner, t   - inner))
    pen.closePath()
    return pen.glyph(), m


def empty_glyph() -> Glyph:
    """Null glyph — zero-length loca entry (correct for space)."""
    return Glyph()


# ── Main ──────────────────────────────────────────────────────────────────────

def build_font():
    print(f"Scanning: {SCRIPT_DIR}")

    glyph_order = [".notdef", "space"]
    cmap        = {0x0020: "space"}
    glyphs_dict = {}
    metrics     = {}
    processed   = []

    for filename, (name, codepoint) in GLYPHS.items():
        path = SCRIPT_DIR / filename
        if not path.exists():
            print(f"  [SKIP] {filename} not found")
            continue

        binary = load_binary(path)
        contours, adv, lsb = image_to_font_contours(binary)

        if not contours:
            print(f"  [WARN] {filename}: no contours found — skipping")
            continue

        glyph_obj = contours_to_ttglyph(contours)
        glyphs_dict[name] = glyph_obj
        metrics[name]     = (adv, lsb)
        glyph_order.append(name)
        cmap[codepoint]   = name
        processed.append((name, codepoint, adv))
        print(f"  {filename:25s} → {name:4s}  U+{codepoint:04X}  adv={adv}")

    if not processed:
        print("ERROR: no glyphs processed. Check that the Myriac Script folder exists.")
        return

    avg_adv = int(sum(a for _, _, a in processed) / len(processed))

    notdef_glyph, notdef_lsb = make_notdef(avg_adv)
    glyphs_dict[".notdef"] = notdef_glyph
    metrics[".notdef"]     = (avg_adv, notdef_lsb)
    glyphs_dict["space"]   = empty_glyph()
    metrics["space"]       = (avg_adv // 3, 0)

    print(f"\nAssembling font ({len(processed)} glyphs, avg advance={avg_adv})…")

    fb = FontBuilder(UNITS_PER_EM, isTTF=True)
    fb.setupGlyphOrder(glyph_order)
    fb.setupCharacterMap(cmap)
    fb.setupGlyf(glyphs_dict)
    fb.setupHorizontalMetrics(metrics)
    fb.setupHorizontalHeader(ascent=ASCENDER, descent=DESCENDER)
    fb.setupNameTable({
        "familyName": "MyriacScript",
        "styleName":  "Regular",
    })
    fb.setupOS2(
        sTypoAscender    = ASCENDER,
        sTypoDescender   = DESCENDER,
        sTypoLineGap     = 0,
        usWinAscent      = ASCENDER,
        usWinDescent     = abs(DESCENDER),
        sxHeight         = X_HEIGHT,
        sCapHeight       = CAP_HEIGHT,
        fsType           = 0,
        achVendID        = "MYRA",
        usWeightClass    = 400,
        usWidthClass     = 5,
        fsSelection      = 0x40,
        ulUnicodeRange1  = 0x00000001,   # Basic Latin
        ulCodePageRange1 = 0x00000001,   # CP 1252 Latin 1
    )
    fb.setupPost()
    fb.setupHead(unitsPerEm=UNITS_PER_EM, macStyle=0)
    fb.setupDummyDSIG()

    gasp = newTable("gasp")
    gasp.version = 1
    gasp.gaspRange = {0xFFFF: 0x000F}  # GRIDFIT | DOGRAY | SYMMETRIC_GRIDFIT | SYMMETRIC_SMOOTHING
    fb.font["gasp"] = gasp

    fb.font.recalcBBoxes = True
    fb.font.save(str(OUTPUT))
    print(f"Saved → {OUTPUT}")


if __name__ == "__main__":
    build_font()
