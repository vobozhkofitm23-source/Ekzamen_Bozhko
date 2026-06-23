"""Generate PowerPoint presentation for Нічний Дозор — centered layout."""
import os
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE

OUT = r"c:\ekz\Presentation_Nichniy_Dozor.pptx"
ASSETS = r"c:\ekz\PresentationAssets"
SLIDE_W = 13.333
SLIDE_H = 7.5

BG = RGBColor(0x08, 0x0E, 0x18)
CARD = RGBColor(0x10, 0x18, 0x28)
ACCENT = RGBColor(0x4A, 0x9E, 0xFF)
GOLD = RGBColor(0xFF, 0xD7, 0x40)
GREEN = RGBColor(0x45, 0xC4, 0x70)
TEXT = RGBColor(0xEA, 0xF0, 0xFA)
DIM = RGBColor(0x8A, 0x98, 0xB0)

RANDOM_ROWS = [
    ("Ряд 1", [
        ("random_row1_10.png", "10", "Контроль території", False),
        ("random_row1_15.png", "15", "Управління простором", False),
        ("random_row1_21.png", "21", "Стартовий вибір", True),
    ]),
    ("Ряд 2", [
        ("random_row2_27.png", "27", "Часові обмеження", True),
        ("random_row2_04.png", "4", "Менеджмент армії", False),
        ("random_row2_13.png", "13", "Дослідження світу", False),
    ]),
    ("Ряд 3", [
        ("random_row3_05.png", "5", "Накопичення ресурсів", False),
        ("random_row3_11.png", "11", "Захист цілі", True),
        ("random_row3_12.png", "12", "Скритність", False),
    ]),
]


def bg(slide):
    f = slide.background.fill
    f.solid()
    f.fore_color.rgb = BG


def center_card(slide, w, h, y=None):
    if y is None:
        y = (SLIDE_H - h) / 2
    x = (SLIDE_W - w) / 2
    sh = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(x), Inches(y), Inches(w), Inches(h))
    sh.fill.solid()
    sh.fill.fore_color.rgb = CARD
    sh.line.color.rgb = ACCENT
    sh.line.width = Pt(1.5)
    return sh


def center_title(slide, title, subtitle=None, y=0.55):
    tb = slide.shapes.add_textbox(Inches(0.8), Inches(y), Inches(SLIDE_W - 1.6), Inches(0.9))
    p = tb.text_frame.paragraphs[0]
    p.text = title
    p.font.size = Pt(36)
    p.font.bold = True
    p.font.color.rgb = ACCENT
    p.alignment = PP_ALIGN.CENTER
    if subtitle:
        sb = slide.shapes.add_textbox(Inches(1.2), Inches(y + 0.65), Inches(SLIDE_W - 2.4), Inches(0.5))
        sp = sb.text_frame.paragraphs[0]
        sp.text = subtitle
        sp.font.size = Pt(16)
        sp.font.color.rgb = DIM
        sp.alignment = PP_ALIGN.CENTER


def center_bullets(slide, items, top=2.0, width=9.5, size=20):
    x = (SLIDE_W - width) / 2
    tb = slide.shapes.add_textbox(Inches(x), Inches(top), Inches(width), Inches(SLIDE_H - top - 0.6))
    tf = tb.text_frame
    tf.word_wrap = True
    tf.vertical_anchor = MSO_ANCHOR.TOP
    for i, line in enumerate(items):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.text = line
        p.font.size = Pt(size)
        p.font.color.rgb = TEXT
        p.alignment = PP_ALIGN.CENTER
        p.space_after = Pt(10)


def slide_title(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_card(s, 9.5, 4.2, 1.65)

    tb = s.shapes.add_textbox(Inches(2.2), Inches(2.5), Inches(9), Inches(2.8))
    tf = tb.text_frame
    p = tf.paragraphs[0]
    p.text = "НІЧНИЙ ДОЗОР"
    p.font.size = Pt(52)
    p.font.bold = True
    p.font.color.rgb = ACCENT
    p.alignment = PP_ALIGN.CENTER

    p2 = tf.add_paragraph()
    p2.text = "Tower Defense · Unity · URP"
    p2.font.size = Pt(22)
    p2.font.color.rgb = GOLD
    p2.alignment = PP_ALIGN.CENTER
    p2.space_before = Pt(12)

    p3 = tf.add_paragraph()
    p3.text = "Ніч + дозор біля кристалу — ти тримаєш оборону до ранку"
    p3.font.size = Pt(17)
    p3.font.color.rgb = DIM
    p3.alignment = PP_ALIGN.CENTER
    p3.space_before = Pt(20)


def add_centered_picture(slide, path, y, width):
    if not os.path.exists(path):
        return
    left = Inches((SLIDE_W - width) / 2)
    slide.shapes.add_picture(path, left, Inches(y), width=Inches(width))


def slide_random(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_title(s, "1. Механіки з RANDOM.ORG", "3 ряди × 3 числа · обрано по 1 з кожного", y=0.32)

    cw, ch = 1.22, 0.98
    gap_x, gap_y = 0.14, 0.18
    grid_w = cw * 3 + gap_x * 2
    grid_h = ch * 3 + gap_y * 2
    sx = (SLIDE_W - grid_w) / 2
    sy = 1.28

    for ri, (rname, cells) in enumerate(RANDOM_ROWS):
        for ci, (fn, num, mech, chosen) in enumerate(cells):
            x = sx + ci * (cw + gap_x)
            y = sy + ri * (ch + gap_y)
            path = os.path.join(ASSETS, fn)
            if os.path.exists(path):
                s.shapes.add_picture(path, Inches(x), Inches(y), width=Inches(cw))
            if chosen:
                fr = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(x - 0.03), Inches(y - 0.03),
                                        Inches(cw + 0.06), Inches(ch + 0.06))
                fr.fill.background()
                fr.line.color.rgb = GREEN
                fr.line.width = Pt(3)
            cap = s.shapes.add_textbox(Inches(x), Inches(y + ch + 0.02), Inches(cw), Inches(0.38))
            cp = cap.text_frame.paragraphs[0]
            cp.text = f"{'✓ ' if chosen else ''}№{num}"
            cp.font.size = Pt(9)
            cp.font.bold = chosen
            cp.font.color.rgb = GREEN if chosen else DIM
            cp.alignment = PP_ALIGN.CENTER

    card_w, card_h = 8.2, 2.25
    card_y = sy + grid_h + 0.55
    center_card(s, card_w, card_h, card_y)

    tb = s.shapes.add_textbox(
        Inches((SLIDE_W - card_w + 0.4) / 2), Inches(card_y + 0.22),
        Inches(card_w - 0.4), Inches(card_h - 0.35))
    tf = tb.text_frame
    lines = [
        ("Обрані механіки", True, GOLD, 20),
        ("", False, TEXT, 6),
        ("21 — Стартовий вибір  ·  27 — Часові обмеження  ·  11 — Захист цілі", True, GREEN, 16),
        ("", False, TEXT, 8),
        ("+ нагорода після 4-ї хвилі (3 випадкових бонуси з 9)", False, DIM, 14),
    ]
    for i, (t, b, c, sz) in enumerate(lines):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.text = t
        p.font.bold = b
        p.font.size = Pt(sz)
        p.font.color.rgb = c
        p.alignment = PP_ALIGN.CENTER


def slide_mechanics(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_title(s, "2. Реалізація в грі", y=0.4)
    center_card(s, 10.5, 5.2, 1.15)
    center_bullets(s, [
        "21 · Стартовий вибір — складність + 3 раси",
        "27 · Таймер хвилі, overtime шкодить кристалу",
        "11 · Захист кристалу — центральна механіка",
        "",
        "Нагорода після 4-ї хвилі:",
        "3 випадкових бонуси з пулу 9 (золото, лучники, HP…)",
        "",
        "6 башен · 10 хвиль · апгрейд · 3 складності · режим АД",
    ], top=1.55, size=18)


def slide_why(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_title(s, "3. Чому обрано ці механіки?", y=0.45)
    center_card(s, 10, 4.8, 1.35)
    center_bullets(s, [
        "Стартовий вибір + захист цілі — класика TD",
        "Таймер додає напругу без надмірної складності",
        "Нагорода на 4-й хвилі — roguelike-елемент середини гри",
        "Простий, але завершений прототип",
    ], top=1.7, size=20)


def slide_inspiration(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_title(s, "4. Надихнення", y=0.45)
    center_card(s, 9.5, 4.5, 1.45)
    center_bullets(s, [
        "Kingdom Rush, Bloons TD",
        "Fantasy: ельфи, гноми, орки, кристал",
        "Kenney low-poly · нічна атмосфера",
        "Roguelike-нагороди на milestone-хвилях",
    ], top=1.75, size=20)


def slide_setting(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_title(s, "5. Сетинг", "Світ «Нічного Дозору»", y=0.4)
    center_card(s, 10.5, 5.0, 1.2)
    center_bullets(s, [
        "Ніч — орди ворогів з трьох порталів",
        "Кристал — те, що треба захистити",
        "«Дозор» — ти чергуєш біля кристалу всю ніч",
        "3 фракції захисників з унікальними бонусами",
        "Після 4-ї хвилі — особлива нагорода дозору",
    ], top=1.55, size=18)


def slide_rewards(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_title(s, "Нагорода за 4-у хвилю", "9 бонусів у базі · 3 випадкових на вибір", y=0.35)
    center_card(s, 11.5, 5.5, 1.05)

    rewards = [
        "Золотий приплив — +200g",
        "Багате полювання — +30% золота за kill",
        "Податок переможців — +40g за хвилю",
        "Швидкі лучники — +15% атака",
        "Важка артилерія — +20% урон гармати/мортири",
        "Крижана буря — slow +0.8с",
        "Розширений дальнобій — +10% range",
        "Майстер апгрейду — −25% ціна",
        "Міцний кристал — +30 HP",
    ]
    col1 = rewards[:5]
    col2 = rewards[5:]
    for col, ox in [(col1, 1.8), (col2, 7.2)]:
        tb = s.shapes.add_textbox(Inches(ox), Inches(1.5), Inches(4.8), Inches(4.8))
        tf = tb.text_frame
        for i, line in enumerate(col):
            p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
            p.text = "• " + line
            p.font.size = Pt(16)
            p.font.color.rgb = TEXT
            p.space_after = Pt(6)


def slide_style(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_title(s, "6. Стиль гри", y=0.45)
    center_card(s, 10, 4.6, 1.45)
    center_bullets(s, [
        "Low-poly 3D · URP · темна нічна палітра",
        "Кольорове кодування башен і UI",
        "Центровані модальні панелі (нагорода, меню)",
        "Ефекти: снаряди, заморозка, зірки апгрейду",
    ], top=1.75, size=20)


def slide_screenshot_menu(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_title(s, "7. Головне меню", "Складність · раса · старт гри", y=0.4)
    add_centered_picture(s, os.path.join(ASSETS, "screenshot_menu.png"), 1.15, 9.2)
    center_bullets(s, [
        "3 рівні складності (Легко / Середнє / АД)",
        "3 раси з унікальними бонусами",
    ], top=6.35, width=10, size=16)


def slide_screenshot_gameplay(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_title(s, "8. Геймплей", "Будівництво башен · захист кристалу", y=0.38)
    add_centered_picture(s, os.path.join(ASSETS, "screenshot_gameplay.png"), 1.05, 10.8)
    center_bullets(s, [
        "6 типів башен · HP-кристал · таймер хвилі · золото",
        "Після 4-ї хвилі — вибір 1 бонусу з 3",
    ], top=6.55, width=11, size=16)


def slide_gameplay(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_title(s, "9. Ігровий процес", y=0.45)
    center_card(s, 10.5, 5.0, 1.2)
    center_bullets(s, [
        "Обрати складність і расу → будувати башні",
        "Відбити 4 хвилі → обрати 1 бонус з 3",
        "Апгрейд / продаж / (АД) ремонт",
        "10 хвиль, таймер, бос, перемога/поразка",
    ], top=1.55, size=19)


def slide_video(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_title(s, "10. Відео геймплею", y=0.5)
    center_card(s, 10, 4.5, 1.5)
    tb = s.shapes.add_textbox(Inches(2.5), Inches(3.2), Inches(8.5), Inches(1.2))
    p = tb.text_frame.paragraphs[0]
    p.text = "▶  ВСТАВТЕ ВІДЕО ТУТ"
    p.font.size = Pt(30)
    p.font.bold = True
    p.font.color.rgb = DIM
    p.alignment = PP_ALIGN.CENTER


def slide_thanks(prs):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    bg(s)
    center_card(s, 8, 2.5, 2.5)
    tb = s.shapes.add_textbox(Inches(2.8), Inches(2.9), Inches(7.8), Inches(1.8))
    tf = tb.text_frame
    p = tf.paragraphs[0]
    p.text = "Дякуємо за увагу!"
    p.font.size = Pt(42)
    p.font.bold = True
    p.font.color.rgb = ACCENT
    p.alignment = PP_ALIGN.CENTER
    p2 = tf.add_paragraph()
    p2.text = "Нічний Дозор"
    p2.font.size = Pt(20)
    p2.font.color.rgb = DIM
    p2.alignment = PP_ALIGN.CENTER


def main():
    prs = Presentation()
    prs.slide_width = Inches(SLIDE_W)
    prs.slide_height = Inches(SLIDE_H)

    slide_title(prs)
    slide_random(prs)
    slide_mechanics(prs)
    slide_why(prs)
    slide_inspiration(prs)
    slide_setting(prs)
    slide_rewards(prs)
    slide_style(prs)
    slide_screenshot_menu(prs)
    slide_screenshot_gameplay(prs)
    slide_gameplay(prs)
    slide_video(prs)
    slide_thanks(prs)

    try:
        prs.save(OUT)
        print(f"Saved: {OUT}")
    except PermissionError:
        alt = OUT.replace(".pptx", "_v2.pptx")
        prs.save(alt)
        print(f"Original locked — saved: {alt}")


if __name__ == "__main__":
    main()
