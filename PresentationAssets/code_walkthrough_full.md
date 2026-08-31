# Повний розбір коду — «Нічний Дозор»

Детальний опис **4 скриптів** у `Assets/Scripts/` та потоку гри.

**Порядок читання:** конфіг → запуск → логіка → сутності → UI.

---

# 0. Структура проєкту

```
Assets/Scripts/
  GameConfig.cs   — баланс, enum, шлях ворогів
  BuildZone.cs    — клітинка будівництва (на сцені)
  Game.cs         — логіка + Tower + Enemy + GameBootstrap
  UIManager.cs    — меню і HUD

Assets/Scenes/SampleScene.unity — карта Level (дорога, BuildZone, кристал)
```

**Потік гри:**

1. `GameBootstrap.Init()` — створює `NightWatch`, камеру зверху.
2. `UIManager` — меню вибору раси.
3. `StartWithRace` — HUD, можна будувати башні.
4. Кнопка «Хвиля» → `SpawnWave` → вороги по `EnemyPath`.
5. Башні в `Update` б'ють найближчого ворога.
6. 5 хвиль без поразки → перемога.

---

# 1. GameConfig.cs

**Роль:** одне джерело всіх чисел.

## Enum-и

| Enum | Значення |
|------|----------|
| `TowerType` | Archer, Cannon |
| `EnemyType` | Scout, Fighter, Tank |
| `Race` | Elf, Dwarf |

## Константи

- `StartGold = 120` — стартове золото
- `CrystalMaxHp = 100` — HP кристала
- `WaveCount = 5` — кількість хвиль
- `GoldPerWave = 38` — бонус після хвилі
- `SpawnInterval = 1.15f` — пауза між ворогами

## Масив хвиль `Waves`

```csharp
(3, 1, 0),  // хвиля 1: 3 скаути, 1 боєць
(2, 2, 0),  // хвиля 2
...
(1, 2, 2)   // хвиля 5
```

## Шлях `EnemyPath`

Масив `Vector3` — світові координати точок. Функція `Cell(x, z)` переводить клітинку сітки в позицію.

## Методи балансу

| Метод | Що повертає |
|-------|-------------|
| `TowerCost` | 50 / 90 |
| `TowerRange` | 12 / 32 |
| `TowerDamage` | 7 / 6 |
| `TowerFireRate` | 1.8 / 0.5 |
| `WaveSeconds` | 45 + wave×5 секунд |
| `EnemyHp` | HP з урахуванням номера хвилі |
| `ApplyRace` | бонус ельфів або гномів |

---

# 2. BuildZone.cs

**Роль:** компонент на зелених клітинках у сцені.

| Поле/метод | Опис |
|------------|------|
| `HasTower` | чи зайнята клітинка |
| `PutTowerHere` | приховати тайл, поставити башню |
| `Free` | звільнити після рестарту |

На об'єкті в сцені також є `BoxCollider` — для raycast при кліку.

---

# 3. Game.cs

**Роль:** головний менеджер + класи `Tower`, `Enemy`, `Shape`, `GameBootstrap`.

## Поля стану `Game`

| Поле | Значення |
|------|----------|
| `PlayerRace` | обрана раса |
| `SelectedTower` | лучник або гармата |
| `Gold`, `CrystalHp` | економіка і ціль |
| `CurrentWave` | номер хвилі |
| `WaveTimeLeft` | таймер активної хвилі |
| `IsPlaying`, `IsGameOver`, `IsWaveActive` | стан гри |
| `BuildZones`, `Enemies` | списки з сцени/runtime |

## Start

1. Знайти `Level` на сцені.
2. `GetComponentsInChildren<BuildZone>()` — усі зони будівництва.
3. Папка `Enemies` для спавну.
4. Створити `EventSystem`, якщо немає.
5. Показати меню.

## Update

- Оновити HUD.
- Якщо хвиля активна — відлік `WaveTimeLeft`, перевірка перемоги на хвилі.
- `HandleClick` — постройка башні.

## Ключові методи

| Метод | Дія |
|-------|-----|
| `StartWithRace` | старт після меню |
| `SelectTower` | вибір типу башні + підсвітка кнопки |
| `StartNextWave` | запуск корутини спавну |
| `ZoneUnderMouse` | raycast + пошук найближчої зони |
| `SpawnWave` | корутина: скаут/боець/танк з паузами |
| `FinishWave` | золото + перемога після 5-ї |
| `EndGame` | поразка або перемога |
| `ResetZones` | очистка башен і ворогів при рестарті |

## Tower (в тому ж файлі)

- `Create` — створити GameObject, примітиви (циліндри), `PutTowerHere`.
- `Update` — cooldown, пошук цілі, `target.Hit(dmg)`.

## Enemy (в тому ж файлі)

- `Create` — спавн на початку `EnemyPath`, додати в `Game.I.Enemies`.
- `Update` — рух по точках; в кінці — урон кристалу.
- `Hit` — отримати урон, при смерті — золото гравцю.

## GameBootstrap

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
static void Init()
```

Автоматично при Play: `NightWatch` + orthographic camera (y=40, size=16).

---

# 4. UIManager.cs

**Роль:** весь інтерфейс у коді.

## Екрани

| Екран | Коли |
|-------|------|
| Меню | старт, рестарт |
| HUD | під час гри |
| End | перемога / поразка |

## HUD елементи

- HP кристала, хвиля, таймер, раса, золото
- Кнопки «Лучник», «Гармата» (з підсвіткою)
- Кнопка «Хвиля»
- Рядок повідомлень (`ShowMessage`)

## RefreshHud

Кожен кадр читає `Game.I` і оновлює тексти. Кнопка хвилі неактивна, поки хвиля йде або гра закінчена.

---

# 5. Одна хвиля — покроково

```
Гравець: «Хвиля»
  → StartNextWave()
  → WaveTimeLeft = 45..65 сек
  → SpawnWave() корутина
      → Enemy.Create × N (з паузою 1.15с)
      → _waveSpawnDone = true

Кожен кадр:
  Enemy.Update → рух по EnemyPath
  Tower.Update → Hit(урон)
  Game.Update → таймер, FinishWave якщо ворогів 0
  UIManager.RefreshHud
```

---

# 6. Що сказати на захисті

«Проєкт спрощений до 4 скриптів. Карта збережена в Unity-сцені, не генерується кодом. `GameConfig` містить баланс і шлях ворогів. `Game` керує хвилями, таймером і кліками. Дві башні, дві раси, 5 хвиль. Башні завдають миттєвий урон без снарядів. UI створюється в `UIManager`. Автозапуск через `GameBootstrap`.»

---

*Проєкт: «Нічний Дозор», 4 скрипти, namespace NightWatch.*
