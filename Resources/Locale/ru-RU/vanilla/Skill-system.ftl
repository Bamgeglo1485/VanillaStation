Skill-issue-message-medicine-unskilled = Требуется { $lvl } ур. медицины
Skill-issue-message-chemistry-unskilled = Требуется { $lvl } ур. химии
Skill-issue-message-rangeweapon-unskilled = Требуется { $lvl } ур. стрельбы
Skill-issue-message-meleeweapon-unskilled = Требуется { $lvl } ур. ближнего боя
Skill-issue-message-engineering-unskilled = Требуется { $lvl } ур. инженерии
Skill-issue-message-crime-unskilled = Требуется { $lvl } ур. преступности
Skill-issue-easyskill-message-piloting-unskilled = Требуется навык пилотирования.
Skill-issue-easyskill-message-botany-unskilled = Требуется навык ботаники.
Skill-issue-easyskill-message-musinstruments-unskilled = Требуется навык музыкальных инструментов.
Skill-issue-easyskill-message-bureaucracy-unskilled = Требуется навык бюрократии.
Skill-issue-easyskill-message-atmosphere-unskilled = Требуется навык атмосферы.
Skill-issue-easyskill-message-crime-unskilled = Требуется навык преступности.
Skill-issue-easyskill-message-research-unskilled = Требуется навык исследований.
shared-solution-container-component-on-examine-main-text-skill-issue = { "" }
construction-menu-skill-building = [color=#FFBF00]Требуется { $lvl } ур. строительства[/color]
construction-menu-skill-engineering = [color=#ff6600]Требуется { $lvl } ур. инженерии[/color]
construction-menu-skill-atmosphere = [color=#4ebed7]Требуется знание атмосферы[/color]
skill-system-bonusskillpoints-message = Вы получили { $skillpoints } очков навыков, т.к. было менее 10 готовых игроков.
examine-skilltrainer-part-2 = доступно { $SkillExpToLearn } опыта.
skill-system-UI-SkillNameLabel = [color=#EFBF04]{ $skillname ->
        [Piloting] Пилотирование
        [RangeWeapon] Стрельба
        [MeleeWeapon] Ближний бой
        [Medicine] Медицина
        [Chemistry] Химия
        [Engineering] Инженерия
        [Building] Строительство
        [Research] Исследование
        [Instrumentation] Приборостроение
        [Botany] Ботаника
        [MusInstruments] Муз. инструменты
        [Bureaucracy] Бюрократия
        [Atmosphere] Атмосфера
        [Crime] Преступность
       *[other] ВТФ?
    }[/color]
skill-system-UI-Lvllabel = Ур. [color={ $color }]{ $lvl }[/color] [color=red]{ $amnesia }[/color]
skill-system-UI-ExpLabel = { $exp }/600[color=red]{ $amnesia }[/color]
skill-system-UI-ExpLabel-maxlvl = Макс.
skill-system-UI-Easy-SkillInfoLabel = Опыт: { $exp }/600
skill-system-UI-Easy-SkillInfoLabel-Have = Изучен
skill-system-UI-Easy-SkillInfoLabel-NotHave = Не изучен
skill-system-UI-Easy-AmnesiaLabel = [color=red]Амнезия: { $exptorestore }[/color]
skill-system-UI-Tooltip-RangeWeapon =
    0 ур:
    - 100% Шанс выронить дробовик, снайперскую винтовку из руки при выстреле
    - 50% Шанс выронить револьвер при выстреле
    - +60° к разбросу оружия
    1 ур:
    - 50% Шанс выронить дробовик или снайперскую винтовку при выстреле
    - Револьвер не выпадает из руки при выстреле
    - +10° к разбросу оружия
    2 ур:
    - Дробовик или снайперская винтовка не выпадает из руки при выстреле
    - Штрафы к разбросу отсутствуют
    3 ур:
    - 30% шанс попасть в голову, что нанесёт урон по выносливости.
skill-system-UI-Tooltip-MeleeWeapon =
    0 ур:
    - Урон: 50%
    - Шансы толкнуть: 10%-25%
    - Длительность заковывания: 3.5с. - 5с.
    1 ур:
    - Урон: 70%
    - Шансы толкнуть: 15%-30%
    - Длительность заковывания: 3с. - 4.5с.
    2 ур:
    - Урон: 100%
    - Шансы толкнуть: 20%-35%
    - Длительность заковывания: 2.5с. - 4с.
    3 ур:
    - 35% шанс моментально встать после оглушения
    - Урон: 100%
    - Шансы толкнуть: 25%-40%
    - Длительность заковывания: 2с. - 3.5с.
skill-system-UI-Tooltip-Medicine =
    0 ур:
    - +100% к длительности дефибрилляции, лечения при помощи наборов от ушибов, бинтов, мази.
    1 ур:
    - +50% к длительности дефибрилляции, лечения при помощи наборов от ушибов, бинтов, мази, регенеративной сети и медицинской нити
    2 ур:
    - Обычная длительность лечения при помощи наборов от ушибов, бинтов, мази, регенеративной сети и медицинской нити.
    - Использование шприцов.
    3 ур:
    - -50% к длительности дефибрилляции, лечения при помощи шприцов, наборов от ушибов, бинтов, мази, регенеративной сети и медицинской нити
    - Крионика
    - Клонирование
    - Введение имплантов
skill-system-UI-Tooltip-Chemistry =
    1 ур:
    - Использование ХимкоМата
    - Использование ХимМастера
    - Использование раздатчика химикатов
    2 ур:
    - Использование очков химического анализа
    - Возможность прочесть этикетку на таблетках, таблетницах, шприцах, кувшинах и мензурках.
    - Распознание свойств веществ
    - Истинный цвет веществ
    3 ур:
    - Использование центрифуги и электролизной установки
    - Распознавание любых веществ
    - 40% Защиты от кислотного урона, ядов и взрывов.
skill-system-UI-Tooltip-Engineering =
    1 ур:
    - Взлом
    - Использование логической системы
    - Генератор гравитации
    - Станционный якорь
    - П.А.К.М.А.Н. И С.У.П.Е.Р.П.А.К.М.А.Н.
    - Крафт электроники
    2 ур:
    - Консоль контроля питания
    - ДАМ
    - Консоль контроля солнечных батарей
    - Разборка гарнитуры
    - Еще больше крафтов!
    3 ур:
    - Тесла, Сингулярность
    - Киборги
    - 75% шанс избежать удара током.
skill-system-UI-Tooltip-Research =
    - Использование сканера аномалий
    - Использование генератора аномалий
    - Использование консоли исследований
    - Использование аналитической консоли
    - Использование А.К.Т.
    - Использование синхронизатора аномалий
skill-system-UI-Tooltip-Crime =
    1 ур:
    - Шаги и инструменты (сварка,лом,кусачки,челюсти итд) становятся намного тише.
    - Шёпот слышен только в радиусе двух тайлов.
    - 20% шанс не оставить никаких отпечатков.
    2 ур:
    - У персонажа появляется скрытое хранилище размером 4х1. Есть шанс 15% выронить предметы при получении урона.
    - 40% шанс не оставить никаких отпечатков.
    3 ур:
    - 60% шанс не оставить никаких отпечатков.
    - Возможность скрытно воровать предметы.
skill-system-UI-Tooltip-Piloting = - Использование консоли управления шаттлом
skill-system-UI-Tooltip-Botany =
    - Взаимодействие с анализатором растений и биофабрикатором
    - Скрещивание растений
    - Сбор семян через секатор
skill-system-UI-Tooltip-Bureaucracy =
    - Возможность легко писать запросы, приказы, отчёты и прочие нормативные документы.
    - Возможноть читать документы
skill-system-UI-Tooltip-Atmosphere =
    - Создание труб, смесителей газов, насосов и прочих атмосферных устройств
    - Взаимодействие с охладителем, нагревателем, насосами, воздушной сигнализацией и прочими атмосферными устройствами
skill-system-UI-Tooltip-MusInstruments = - Игра на музыкальных инструментах
