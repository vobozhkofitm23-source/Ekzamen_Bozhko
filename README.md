# Нічний Дозор — Tower Defense (Unity)

Tower defense для захисту курсової / екзамену. Проєкт спрощений до **4 скриптів**, карта збережена в сцені.

## Механіки

| Механіка | Реалізація |
|----------|------------|
| Стартовий вибір | 2 раси: Ельфи (+15% швидкість атаки), Гноми (+20% урон) |
| Будівництво | 2 башні: Лучник, Гармата |
| Захист цілі | Кристал (100 HP), вороги йдуть по фіксованому шляху |
| Хвилі | 5 хвиль, таймер на кожну; якщо час вийшов — поразка |
| Карта | Об'єкт `Level` у `SampleScene` (не генерується кодом) |

## Скрипти

| Файл | Роль |
|------|------|
| `GameConfig.cs` | Баланс, шлях ворогів, склад хвиль |
| `BuildZone.cs` | Зелені клітинки на сцені |
| `Game.cs` | Логіка, башні, вороги |
| `UIManager.cs` | Меню, HUD, екран кінця гри |

## Керування

1. Обрати расу в меню
2. Натиснути **Лучник** або **Гармата** (кнопка підсвічується зеленим)
3. Клік по зеленій клітинці на карті — побудувати башню
4. **Хвиля** — запустити наступну хвилю ворогів
5. Знищити всіх ворогів до кінця таймера

## Запуск

Відкрийте Unity → сцена `Assets/Scenes/SampleScene.unity` → **Play**

На сцені вже є:
- `Level` — карта
- `Main Camera` — вид зверху
- `NightWatch` — скрипти `Game` і `UIManager`

## Документація для захисту

- `PresentationAssets/Presentation_Nichniy_Dozor.pptx` — презентація
- `PresentationAssets/exam_prep_code_qa.pdf` — питання та відповіді
- `PresentationAssets/code_walkthrough_full.pdf` — розбір коду по файлах

## Структура проєкту

```
Assets/Scripts/          — 4 скрипти гри
Assets/Scenes/           — SampleScene (карта + NightWatch)
Assets/Resources/Models/ — моделі карти (tile, дорога, кристал)
PresentationAssets/      — презентація та PDF для захисту
```

## Ассети (тільки потрібні)

Kenney Tower Defense Kit (CC0) — `Assets/Resources/Models/`:
- `tile.fbx` — трава
- `tile-dirt.fbx` — дорога
- `spawn-round.fbx` — спавн
- `snow-detail-crystal-large.fbx` — кристал
- `Textures/variation-a.png` — зелені зони будівництва
