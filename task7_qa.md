# Завдання 7. Manual QA: US11 Guest Viewing Reservation Menu

Об'єкт тестування: user story **[Draft] US11_Guest Viewing Reservation Menu** (Epic: Order Management),
скріншот реалізованої сторінки Menu (гість зайшов за посиланням `/join/{token}`).

Тест-кейси й дефект-репорти оформлені за шаблоном у Google-таблиці (вкладки *Test cases* і *Defect reports*):
https://docs.google.com/spreadsheets/d/1r6qOlsd7TrJNGREEbMK78q9DnWW3Q9LjUGHU9buSfsM/edit?usp=sharing
Нижче той самий вміст для зручності.

---

## 1. Аналіз вимог: знайдені недоліки

### Оформлення та структура
1. **Назви не збігаються.** У заголовку «Guest Viewing Reservation Menu», а в полі *US name* «Guest Reservation and Menu View». Незрозуміло, чи входить у story перегляд деталей бронювання.
2. **Статус «Draft».** Story ще не затверджена, але її вже реалізовано й тестують.
3. **Зламана нумерація AC.** AC5 порожній, а текст «The menu reflects the current data…» стоїть окремо без номера. Незрозуміло, чи це AC5 і чи не загубився якийсь критерій.
4. **Немає посилання на дизайн (Figma)** і на пов'язані story (US_G10 згадана, але без посилання; story про кошик гостя не згадана).
5. **Немає пріоритету, оцінки та Definition of Done.**

### Передумови
6. **Статуси не відповідають системі.** Написано «Created/"Reserved"», але в застосунку reservation має статус `Reserved`, а group reservation має `Open / Submitted / Closed`. Статусу `Created` для бронювання немає. Незрозуміло, який саме статус перевіряти.
7. **Не описано, що бачить гість, коли умова не виконується:** бронювання скасоване або завершене, Owner уже відправив груповий pre-order (`Submitted`), гість уже підтвердив свій кошик (`isSubmitted = true`).

### Критерії приймання
8. **AC1: перелік категорій нечіткий («e.g.»).** Не визначено точний набір і порядок вкладок, наявність вкладки «All dishes» (на реалізації вона є), поведінку порожньої категорії (показувати вкладку чи ховати). Назви теж розходяться: у story «Main Course», на сторінці «Main Courses».
9. **AC1: «structured logically» не можна перевірити.** Потрібне конкретне правило, наприклад порядок категорій і сортування в межах категорії. Сортування «Sort by: Recommended» реалізоване, але в story його немає.
10. **AC2: не визначено список статусів доступності.** У системі їх три (`Available`, `On Stop`, `Out of Stock`), а story згадує лише «On Stop». Невідомо, чи показувати бейдж для доступної страви.
11. **AC2: немає формату даних.** Валюта і точність ціни ($12.00), формат ваги («200g» чи «200 g»), що показувати, коли немає зображення (плейсхолдер).
12. **AC3: кнопка «e.g. "Pre-order"».** Назву кнопки не зафіксовано. Не описано:
    - що бачить гість після додавання (toast, лічильник на іконці кошика);
    - що відбувається при повторному натисканні (кількість +1 чи друга позиція);
    - чи є ліміт кількості;
    - що таке «personal subcart» і де він відкривається.
13. **AC4: згадано лише «On Stop».** Поведінку для «Out of Stock» не описано, хоча на реалізації саме цей статус. «Clearly indicated» не має візуальної специфікації (бейдж, сіра картка, неактивна кнопка).
14. **AC5: «актуальні дані» без деталей.** Не визначено:
    - чи оновлюється меню автоматично, чи лише після refresh;
    - що станеться зі стравою, яка вже в кошику гостя, якщо вона стала On Stop;
    - що станеться, якщо ціна змінилася після додавання.
15. **Немає негативних і граничних сценаріїв:** меню не завантажилося (помилка API), у локації немає страв, сесія гостя прострочена під час перегляду, повільна мережа (стан завантаження).
16. **Не описано контекст гостя на сторінці.** Що показує банер бронювання (адреса, стіл, **дата**, час)? Які пункти навігації бачить неавторизований гість («Book a Table», «Reservations», іконка профілю)?
17. **Немає нефункціональних вимог:** адаптивність (mobile), доступність, час завантаження меню.

### Відкриті питання до PO/BA
- Чи показувати страви зі статусом Out of Stock і On Stop, чи ховати їх?
- Чи може гість відкрити деталі страви (склад, калорії), натиснувши на картку?
- Чи може гість змінювати кошик після того, як підтвердив його?
- Чи бачить гість, що вже обрали інші учасники групи?

---

## 2. Тест-кейси

Статуси виставлено за результатами перевірки на наданому скріншоті реалізації:
**Passed** (перевірено, працює), **Failed** (знайдено дефект), **Not run** (скріншот не дає змоги перевірити, бо потрібна взаємодія або інші дані).

Загальна передумова для всіх кейсів: Owner згенерував і поширив валідне посилання-запрошення; reservation = `Reserved`, group reservation = `Open`; гість приєднався до сесії (US_G10).

| ID | Title | Pre-conditions | Steps | Expected result | Status |
|---|---|---|---|---|---|
| 1 | Verify that Menu page is opened for Guest after joining the group session | Common | 1. Open invite link. 2. Enter name, click [Browse menu]. | Menu page is opened: header, reservation banner, breadcrumbs «Main page › Menu», «Menu» title, category tabs, dish cards. | Passed |
| 2 | Verify that menu is grouped by categories | Common; location has dishes in each category | 1. Open Menu page. 2. Look at the category tabs. | Tabs «All dishes», «Appetizers», «Main Courses», «Desserts» are shown; «All dishes» is selected by default. | Not run (no Desserts tab on the screenshot; can't confirm whether the location has desserts) |
| 3 | Verify that only dishes of the selected category are shown | Common | 1. Click [Appetizers]. 2. Click [Main Courses]. 3. Click [All dishes]. | 1. Only appetizers are shown, the tab is underlined. 2. Only main courses are shown. 3. All dishes are shown. | Not run |
| 4 | Verify that each dish card shows name, price, weight, image and availability status | Common | 1. Open Menu page. 2. Check every dish card. | Every card has the dish's own image, name, price, weight in grams and availability status, per Figma. | Failed (defects 1, 2) |
| 5 | Verify price format on dish cards | Common | 1. Check the price on every card. | Price is shown as $X.XX (e.g. $12.00, $22.50, $14.99). | Passed |
| 6 | Verify weight format on dish cards | Common | 1. Check the weight on every card. | Weight is in grams in one format on all cards (e.g. «200 g»). | Failed (defect 3) |
| 7 | Verify that available dish has an active [Pre-Order] button | Dish is Available | 1. Find an available dish (Margherita Pizza). | [Pre-Order] button is enabled and highlighted (green). | Passed |
| 8 | Verify that Guest can add an available dish to the subcart | Dish is Available; Guest cart is empty | 1. Click [Pre-Order] on an available dish. 2. Open the cart icon. | 1. Confirmation is shown, cart counter = 1. 2. Dish is in the Guest's subcart with quantity 1 and its price. | Not run |
| 9 | Verify that Out of Stock dish is shown as unavailable | Dish is Out of Stock | 1. Find an Out of Stock dish (Fresh Avocado Toast). | «Out of stock» badge is shown, card is greyed out, [Pre-Order] is disabled. | Passed (badge text casing: defect 4) |
| 10 | Verify that On Stop dish is shown as unavailable and can't be added | Dish is On Stop | 1. Find an On Stop dish. 2. Click [Pre-Order]. | 1. «On Stop» badge is shown, card is greyed out. 2. Button is disabled, nothing is added to the cart. | Not run (no On Stop dish in test data) |
| 11 | Verify that disabled [Pre-Order] can't add a dish | Dish is Out of Stock | 1. Click disabled [Pre-Order]. 2. Open the cart. | Nothing happens, dish is not in the cart. | Not run |
| 12 | Verify that menu shows current data after refresh | Menu page is opened | 1. Admin sets an available dish to On Stop. 2. Guest refreshes the page. | Dish is shown as On Stop with disabled [Pre-Order]. | Not run |
| 13 | Verify reservation banner on Menu page | Common | 1. Open Menu page. 2. Read the green banner. | Banner shows address, table, date and time of the reservation. | Failed (defect 5) |
| 14 | Verify that Guest can't open the menu when reservation is cancelled | Reservation = Cancelled | 1. Open invite link / refresh Menu page. | Static page «This pre-order session has expired or is no longer active.» is shown, menu isn't available. | Not run |
| 15 | Verify that Guest can't add dishes after Owner submitted the group pre-order | Group reservation = Submitted | 1. Open Menu page. 2. Click [Pre-Order]. | Message that the group pre-order is already submitted; dish isn't added. | Not run |
| 16 | Verify empty category state | Location has no dishes in a category | 1. Click that category tab. | Empty-state message is shown, no broken layout. | Not run |

---

## 3. Дефект-репорти

Env: P_PRD (`https://d28jftogiwtein.cloudfront.net`), гостьова сесія за посиланням `/join/613fc984…`.

### Defect 1. All dishes on the Menu page show the same image
- **Priority:** High · **Severity:** Major
- **Pre-conditions:** Guest joined the group session; the menu has several different dishes.
- **Steps:** 1. Open Menu page as Guest. 2. Compare the images on the dish cards.
- **Actual result:** «Fresh Avocado Toast», «Truffle Mushroom Risotto» and «Margherita Pizza» all show the same strawberry-salad image.
- **Expected result:** Every dish card shows the image of that dish (AC2).
- **Attachments:** screenshot of the Menu page.

### Defect 2. Availability status isn't shown for available dishes
- **Priority:** Medium · **Severity:** Minor
- **Pre-conditions:** Guest joined the group session; the menu has an available dish.
- **Steps:** 1. Open Menu page as Guest. 2. Look at the «Margherita Pizza» card.
- **Actual result:** The card has no availability status; only unavailable dishes have a badge.
- **Expected result:** Each menu item displays its current availability status (AC2).
- **Attachments:** screenshot of the Menu page.

### Defect 3. Dish weight is shown in different formats
- **Priority:** Low · **Severity:** Trivial
- **Pre-conditions:** Guest joined the group session.
- **Steps:** 1. Open Menu page as Guest. 2. Compare the weight under the prices.
- **Actual result:** «200g» and «350g» (no space) vs «450 g» (with a space).
- **Expected result:** One weight format on all cards (e.g. «200 g»).
- **Attachments:** screenshot of the Menu page.

### Defect 4. Status badge text is in wrong case: «Out Of Stock»
- **Priority:** Low · **Severity:** Trivial
- **Pre-conditions:** Guest joined the group session; a dish is Out of Stock.
- **Steps:** 1. Open Menu page as Guest. 2. Look at the badge on «Fresh Avocado Toast».
- **Actual result:** Badge text is «Out Of Stock».
- **Expected result:** «Out of stock» (sentence case, per Figma and the rest of the UI).
- **Attachments:** screenshot of the Menu page.

### Defect 5. Reservation banner doesn't show the reservation date
- **Priority:** Medium · **Severity:** Minor
- **Pre-conditions:** Guest joined the group session.
- **Steps:** 1. Open Menu page as Guest. 2. Read the green banner under the header.
- **Actual result:** «You are making a pre-order for the reservation 48 Rustaveli Avenue, Table 1, 15:30 - 17:00»: no date.
- **Expected result:** Banner shows the reservation date together with the time, so the Guest can check which reservation they are ordering for.
- **Attachments:** screenshot of the Menu page.
