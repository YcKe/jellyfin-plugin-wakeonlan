"""Render the plugin logo PNGs from the same geometry as assets/logo.svg.

Usage:  python tools/make_logo.py
Writes: assets/logo.png (1280x720 catalog banner) and assets/icon.png (512x512).

Only Pillow is required. Everything is drawn at 4x and downsampled for clean edges.
"""

from __future__ import annotations

import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(ROOT, "assets")

PURPLE = (155, 79, 191)
BLUE = (26, 143, 208)
WHITE = (255, 255, 255)
SCALE = 4

FONT_CANDIDATES_BOLD = ["segoeuib.ttf", "arialbd.ttf", "DejaVuSans-Bold.ttf"]
FONT_CANDIDATES_REGULAR = ["segoeui.ttf", "arial.ttf", "DejaVuSans.ttf"]


def load_font(candidates: list[str], size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for name in candidates:
        for folder in ("", "C:/Windows/Fonts", "/usr/share/fonts/truetype/dejavu"):
            try:
                return ImageFont.truetype(os.path.join(folder, name), size)
            except OSError:
                continue
    return ImageFont.load_default()


def gradient(width: int, height: int) -> Image.Image:
    """Left-to-right purple to blue gradient."""
    ramp = Image.linear_gradient("L").rotate(90, expand=True).resize((width, height))
    return Image.composite(Image.new("RGB", (width, height), BLUE), Image.new("RGB", (width, height), PURPLE), ramp)


def rounded_mask(width: int, height: int, radius: int) -> Image.Image:
    mask = Image.new("L", (width, height), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, width - 1, height - 1), radius=radius, fill=255)
    return mask


def draw_glyph(mask_draw: ImageDraw.ImageDraw, cx: int, cy: int, unit: float) -> None:
    """Crescent moon with three signal arcs radiating to the right. `unit` scales the glyph."""
    moon_r = int(150 * unit)
    cut_r = int(130 * unit)
    cut_dx, cut_dy = int(60 * unit), int(-60 * unit)
    mask_draw.ellipse((cx - moon_r, cy - moon_r, cx + moon_r, cy + moon_r), fill=255)
    mask_draw.ellipse((cx + cut_dx - cut_r, cy + cut_dy - cut_r, cx + cut_dx + cut_r, cy + cut_dy + cut_r), fill=0)

    arc_cx = cx + int(30 * unit)
    stroke = int(26 * unit)
    for radius in (230, 300, 370):
        r = int(radius * unit)
        mask_draw.arc((arc_cx - r, cy - r, arc_cx + r, cy + r), start=-40, end=40, fill=255, width=stroke)


def render_banner() -> Image.Image:
    w, h = 1280 * SCALE, 720 * SCALE
    base = gradient(w, h)
    mask = Image.new("L", (w, h), 0)
    draw = ImageDraw.Draw(mask)

    draw_glyph(draw, 300 * SCALE, 360 * SCALE, SCALE)

    title = load_font(FONT_CANDIDATES_BOLD, 84 * SCALE)
    subtitle = load_font(FONT_CANDIDATES_REGULAR, 40 * SCALE)
    draw.text((700 * SCALE, 335 * SCALE), "Wake-on-LAN", font=title, fill=255, anchor="lm")
    draw.text((704 * SCALE, 418 * SCALE), "for Jellyfin", font=subtitle, fill=200, anchor="lm")

    base.paste(WHITE, mask=mask)
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out.paste(base, mask=rounded_mask(w, h, 48 * SCALE))
    return out.resize((1280, 720), Image.LANCZOS)


def render_icon() -> Image.Image:
    w = h = 512 * SCALE
    base = gradient(w, h)
    mask = Image.new("L", (w, h), 0)
    draw = ImageDraw.Draw(mask)

    # Glyph spans roughly 520 units wide; scale it to fit with padding and centre it.
    unit = SCALE * 0.72
    draw_glyph(draw, int(w / 2 - 120 * unit), h // 2, unit)

    base.paste(WHITE, mask=mask)
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out.paste(base, mask=rounded_mask(w, h, 96 * SCALE))
    return out.resize((512, 512), Image.LANCZOS)


def main() -> int:
    os.makedirs(ASSETS, exist_ok=True)
    render_banner().save(os.path.join(ASSETS, "logo.png"), optimize=True)
    render_icon().save(os.path.join(ASSETS, "icon.png"), optimize=True)
    print("wrote assets/logo.png and assets/icon.png")
    return 0


if __name__ == "__main__":
    sys.exit(main())
