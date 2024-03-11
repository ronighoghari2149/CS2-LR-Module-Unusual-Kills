# RU
[C#] [LR] Module Unusual Kills это модуль для плагина [C#] [Levels Ranks] Core. Данный модуль выдаёт дополнительные очки опыта за необычные убийства. К примеру: прострелом, без прицела с awp, в прыжке, с ослеплением от световой гранаты, в смок, на бегу.

# Установка
1. Установите [C# Levels Ranks Core](https://github.com/ABKAM2023/CS2-LevelsRanks-Core)
2. Скачайте [C#] [LR] Module Unusual Kills
3. Распакуйте архив и загрузите его на игровой сервер
4. Перезапустите сервер

# Основной конфигурационный файл (UnusualKills.yml)
```
# Настройка '[LR] Module Unusual Kills'

# Количество очков за первое убийство
PointsForFirstKill: 1
# Количество очков за убийство прострелом
PointsForWallbang: 1
# Количество очков за убийство с AWP без прицела
PointsForNoScopeAWP: 2
# Количество очков за убийство на бегу
PointsForKillWhileRunning: 2
# Количество очков за убийство в пржыке
PointsForJumpKill: 2
# Количество очков за убийство ослеплённым
PointsForBlindKill: 2
# Количество очков за убийство через смоук
PointsForSmokeKill: 5
```

# Конфигурационный файл со сообщениями плагина (UnusualKills.phrases.yml)
```
# Сообщения модуля '[LR] Module Unusual Kills'

# Сообщение, отображаемое при убийстве через смоук
FirstKillMessage: "первое убийство в раунде"

# Цвет сообщения, отображаемого при убийстве через смоук
FirstKillMessageColor : "{Green}"

# Сообщение, отображаемое при убийстве через смоук
WallbangKillMessage: "прострел"

# Цвет сообщения, отображаемого при убийстве через смоук
WallbangKillMessageColor : "{Green}"

# Сообщение, отображаемое при убийстве с AWP без прицела
NoScopeAWPMessage: "убийство без прицела"

# Цвет сообщения, отображаемого при убийстве с AWP без прицела
NoScopeAWPMessageColor: "{Green}"

# Сообщение, отображаемое при убийстве на бегу
KillWhileRunningMessage: "убийство на бегу"

# Цвет сообщения, отображаемого при убийстве на бегу
KillWhileRunningMessageColor: "{Green}"

# Сообщение, отображаемое при убийстве в прыжке
JumpKillMessage: "убийство в прыжке"

# Цвет сообщения, отображаемого при убийстве в прыжке
JumpKillMessageColor : "{Olive}"

# Сообщение, отображаемое при убийстве ослеплённым
BlindKillMessage: "убийство ослеплённым"

# Цвет сообщения, отображаемого при убийстве ослеплённым
BlindKillMessageColor : "{Green}"

# Сообщение, отображаемое при убийстве через смоук
SmokeKillMessage: "убийство через дым"

# Цвет сообщения, отображаемого при убийстве через смоук
SmokeKillMessageColor : "{Grey}"
```
