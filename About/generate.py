#!/usr/bin/env python3
"""Regenerate Preview.png and ModIcon.png from their SVG/screenshot sources.

Usage: ./generate.py

Preview.png: renders LogoText_Power.svg and LogoText_Diode.svg with rsvg-convert,
crops/rescales the screenshot to 640x360, and composites the two words (with a soft
drop shadow) onto it: POWER centered above the output battery, DIODE (with its
RimWorld-style O) centered below it, both an equal distance from the top/bottom edges.

ModIcon.png: rasterizes Common/Textures/Buildings/PowerDiodeDrawNode.svg to 64x64.
"""
import subprocess
from pathlib import Path

from PIL import Image, ImageFilter

ABOUT_DIR = Path(__file__).parent
SCREENSHOT = ABOUT_DIR / "Screenshot From 2026-09-03 15-24-07.png"
DRAW_NODE_SVG = ABOUT_DIR.parent / "Common" / "Textures" / "Buildings" / "PowerDiodeDrawNode.svg"
PREVIEW_SIZE = (640, 360)
CENTER_X = 486
TOP_MARGIN = 22
WORD_WIDTH = 300  # target render width for "Power"; "diOde" is scaled to match
MOD_ICON_SIZE = 64


def render_svg_trimmed(svg_path: Path) -> Image.Image:
    raw_path = svg_path.with_suffix(".raw.png")
    subprocess.run(
        [
            "rsvg-convert",
            "-w", "2000",
            "-h", "400",
            "--keep-aspect-ratio",
            "-b", "none",
            str(svg_path),
            "-o", str(raw_path),
        ],
        check=True,
    )
    im = Image.open(raw_path)
    im = im.crop(im.getbbox())
    raw_path.unlink()
    return im


def crop_and_scale_screenshot() -> Image.Image:
    im = Image.open(SCREENSHOT).convert("RGBA")
    w, h = im.size
    target_ratio = PREVIEW_SIZE[0] / PREVIEW_SIZE[1]
    new_w = round(h * target_ratio)
    left = (w - new_w) // 2
    im = im.crop((left, 0, left + new_w, h))
    return im.resize(PREVIEW_SIZE, Image.LANCZOS)


def add_shadow(word_img: Image.Image, pos: tuple[int, int], shadow_layer: Image.Image) -> None:
    alpha = word_img.split()[3]
    shadow = Image.new("RGBA", word_img.size, (0, 0, 0, 0))
    shadow.putalpha(alpha.point(lambda a: int(a * 0.55)))
    shadow_layer.paste(shadow, (pos[0] + 3, pos[1] + 4), shadow)


def generate_mod_icon() -> None:
    subprocess.run(
        [
            "magick",
            "-background", "none",
            "-density", "384",
            str(DRAW_NODE_SVG),
            "-resize", f"{MOD_ICON_SIZE}x{MOD_ICON_SIZE}",
            str(ABOUT_DIR / "ModIcon.png"),
        ],
        check=True,
    )


def generate_preview() -> None:
    bg = crop_and_scale_screenshot()

    power = render_svg_trimmed(ABOUT_DIR / "LogoText_Power.svg")
    diode = render_svg_trimmed(ABOUT_DIR / "LogoText_Diode.svg")

    scale = WORD_WIDTH / power.width
    power = power.resize((int(power.width * scale), int(power.height * scale)), Image.LANCZOS)
    diode = diode.resize((int(diode.width * scale), int(diode.height * scale)), Image.LANCZOS)

    power_pos = (CENTER_X - power.width // 2, TOP_MARGIN)
    diode_pos = (CENTER_X - diode.width // 2, bg.height - TOP_MARGIN - diode.height)

    shadow_layer = Image.new("RGBA", bg.size, (0, 0, 0, 0))
    add_shadow(power, power_pos, shadow_layer)
    add_shadow(diode, diode_pos, shadow_layer)
    shadow_layer = shadow_layer.filter(ImageFilter.GaussianBlur(2.5))

    composed = Image.alpha_composite(bg, shadow_layer)
    word_layer = Image.new("RGBA", bg.size, (0, 0, 0, 0))
    word_layer.paste(power, power_pos, power)
    word_layer.paste(diode, diode_pos, diode)
    composed = Image.alpha_composite(composed, word_layer)

    composed.convert("RGB").save(ABOUT_DIR / "Preview.png")


def main() -> None:
    generate_preview()
    generate_mod_icon()


if __name__ == "__main__":
    main()
