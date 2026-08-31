# -*- coding: utf-8 -*-
"""Generate PDF from exam_prep_code_qa.md (Ukrainian text)."""
import re
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    HRFlowable,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)

ROOT = Path(__file__).parent
DEFAULT_MD = ROOT / "exam_prep_code_qa.md"
DEFAULT_OUT = ROOT / "exam_prep_code_qa.pdf"

FONT = Path(r"C:\Windows\Fonts\arial.ttf")
FONT_BOLD = Path(r"C:\Windows\Fonts\arialbd.ttf")


def register_fonts():
    pdfmetrics.registerFont(TTFont("Arial", str(FONT)))
    pdfmetrics.registerFont(TTFont("Arial-Bold", str(FONT_BOLD)))


def esc(text: str) -> str:
    text = text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    text = re.sub(r"`([^`]+)`", r'<font face="Courier">\1</font>', text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", text)
    return text


def styles():
    return {
        "title": ParagraphStyle(
            "title",
            fontName="Arial-Bold",
            fontSize=18,
            leading=22,
            alignment=TA_CENTER,
            spaceAfter=6,
        ),
        "subtitle": ParagraphStyle(
            "subtitle",
            fontName="Arial",
            fontSize=12,
            leading=16,
            alignment=TA_CENTER,
            spaceAfter=10,
            textColor=colors.HexColor("#333333"),
        ),
        "h1": ParagraphStyle(
            "h1",
            fontName="Arial-Bold",
            fontSize=15,
            leading=19,
            spaceBefore=10,
            spaceAfter=6,
            textColor=colors.HexColor("#1e3c78"),
        ),
        "h2": ParagraphStyle(
            "h2",
            fontName="Arial-Bold",
            fontSize=12,
            leading=15,
            spaceBefore=8,
            spaceAfter=4,
        ),
        "body": ParagraphStyle(
            "body",
            fontName="Arial",
            fontSize=10,
            leading=14,
            spaceAfter=4,
        ),
        "code": ParagraphStyle(
            "code",
            fontName="Courier",
            fontSize=8.5,
            leading=11,
            leftIndent=8,
            spaceAfter=4,
            backColor=colors.HexColor("#f4f4f4"),
        ),
        "footer": ParagraphStyle(
            "footer",
            fontName="Arial",
            fontSize=8,
            leading=10,
            alignment=TA_CENTER,
            textColor=colors.grey,
        ),
    }


def parse_table(lines: list[str]) -> Table | None:
    if len(lines) < 2 or "|" not in lines[0]:
        return None
    rows = []
    for line in lines:
        if re.match(r"^\|[-:\s|]+\|$", line.strip()):
            continue
        cells = [esc(c.strip()) for c in line.strip().strip("|").split("|")]
        rows.append(cells)
    if not rows:
        return None
    col_count = max(len(r) for r in rows)
    data = []
    for i, row in enumerate(rows):
        while len(row) < col_count:
            row.append("")
        data.append([Paragraph(c, styles()["body"]) for c in row])
    tbl = Table(data, hAlign="LEFT")
    style_cmds = [
        ("GRID", (0, 0), (-1, -1), 0.5, colors.grey),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 4),
        ("RIGHTPADDING", (0, 0), (-1, -1), 4),
        ("TOPPADDING", (0, 0), (-1, -1), 3),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
    ]
    if data:
        style_cmds.append(("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#e8eef8")))
        style_cmds.append(("FONTNAME", (0, 0), (-1, 0), "Arial-Bold"))
    tbl.setStyle(TableStyle(style_cmds))
    return tbl


def md_to_flowables(text: str) -> list:
    st = styles()
    flow = []
    lines = text.splitlines()
    i = 0
    first_h1 = True

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        if not stripped:
            i += 1
            continue

        if stripped == "---":
            flow.append(Spacer(1, 4))
            flow.append(HRFlowable(width="100%", thickness=0.5, color=colors.lightgrey))
            flow.append(Spacer(1, 4))
            i += 1
            continue

        if stripped.startswith("```"):
            code_lines = []
            i += 1
            while i < len(lines) and not lines[i].strip().startswith("```"):
                code_lines.append(lines[i].replace("<", "&lt;").replace(">", "&gt;"))
                i += 1
            i += 1
            for cl in code_lines:
                flow.append(Paragraph(cl or " ", st["code"]))
            flow.append(Spacer(1, 4))
            continue

        if stripped.startswith("|") and i + 1 < len(lines) and "|" in lines[i + 1]:
            table_lines = []
            while i < len(lines) and lines[i].strip().startswith("|"):
                table_lines.append(lines[i].strip())
                i += 1
            tbl = parse_table(table_lines)
            if tbl:
                flow.append(tbl)
                flow.append(Spacer(1, 6))
            continue

        if stripped.startswith("# "):
            title = esc(stripped[2:])
            if first_h1:
                flow.append(Paragraph(title, st["title"]))
                first_h1 = False
            else:
                flow.append(PageBreak())
                flow.append(Paragraph(title, st["h1"]))
            i += 1
            continue

        if stripped.startswith("## "):
            flow.append(Paragraph(esc(stripped[3:]), st["h2"]))
            i += 1
            continue

        if stripped.startswith("- "):
            flow.append(Paragraph(f"• {esc(stripped[2:])}", st["body"]))
            i += 1
            continue

        if re.match(r"^\d+\.\s", stripped):
            flow.append(Paragraph(esc(stripped), st["body"]))
            i += 1
            continue

        if stripped.startswith("*") and stripped.endswith("*"):
            flow.append(Paragraph(esc(stripped.strip("*")), st["footer"]))
            i += 1
            continue

        flow.append(Paragraph(esc(stripped), st["body"]))
        i += 1

    return flow


def add_page_number(canvas, doc, footer: str):
    canvas.saveState()
    canvas.setFont("Arial", 8)
    canvas.setFillColor(colors.grey)
    canvas.drawCentredString(A4[0] / 2, 12 * mm, f"{footer} — стор. {canvas.getPageNumber()}")
    canvas.restoreState()


def build_pdf(md_path: Path, out_path: Path, title: str, footer: str):
    if not md_path.exists():
        raise SystemExit(f"Markdown not found: {md_path}")
    register_fonts()
    text = md_path.read_text(encoding="utf-8")
    flow = md_to_flowables(text)

    doc = SimpleDocTemplate(
        str(out_path),
        pagesize=A4,
        leftMargin=18 * mm,
        rightMargin=18 * mm,
        topMargin=16 * mm,
        bottomMargin=18 * mm,
        title=title,
        author="NightWatch",
    )
    doc.build(
        flow,
        onFirstPage=lambda c, d: add_page_number(c, d, footer),
        onLaterPages=lambda c, d: add_page_number(c, d, footer),
    )
    print(f"PDF created: {out_path}")


def main():
    import argparse

    parser = argparse.ArgumentParser(description="Generate PDF from markdown (Ukrainian).")
    parser.add_argument("--input", type=Path, default=DEFAULT_MD)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUT)
    parser.add_argument("--title", default="Підготовка до захисту — Нічний Дозор")
    parser.add_argument("--footer", default="Нічний Дозор")
    parser.add_argument("--all", action="store_true", help="Build exam Q&A and code walkthrough PDFs")
    args = parser.parse_args()

    if args.all:
        build_pdf(
            ROOT / "exam_prep_code_qa.md",
            ROOT / "exam_prep_code_qa.pdf",
            "Підготовка до захисту — Нічний Дозор",
            "Нічний Дозор — Q&A",
        )
        build_pdf(
            ROOT / "code_walkthrough_full.md",
            ROOT / "code_walkthrough_full.pdf",
            "Повний розбір коду — Нічний Дозор",
            "Нічний Дозор — код",
        )
        build_pdf(
            ROOT / "full_code_guide.md",
            ROOT / "full_code_guide.pdf",
            "Повний гайд по коду — Нічний Дозор",
            "Нічний Дозор — гайд",
        )
        return

    build_pdf(args.input, args.output, args.title, args.footer)


if __name__ == "__main__":
    main()
