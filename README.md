# ConferenceRoomBooking

API та демонстраційний вебінтерфейс для керування конференц-залами, пошуку вільного часу, бронювання з автоматичним розрахунком вартості та перегляду бізнес-звітів.

Опис можливостей, тарифів, звітів і технічних рішень: [коротка документація проєкту](PROJECT_DOCUMENTATION.md).

## Запуск проєкту на новому Windows-пристрої

Для запуску встановіть:

- **Git:** [офіційна сторінка завантаження](https://git-scm.com/install/).
- **Docker Desktop:** [офіційна сторінка завантаження](https://www.docker.com/products/docker-desktop/).

Запустіть Docker Desktop і переконайтеся, що використовуються **Linux containers**. Локально встановлювати MySQL або .NET SDK для запуску через Docker не потрібно.

Відкрийте PowerShell у папці, куди потрібно завантажити проєкт, і виконайте:

```powershell
git clone https://github.com/Alexander7890/ConferenceRoomBooking.git
cd ConferenceRoomBooking
Copy-Item .env.example .env
```

Після створення `.env` за потреби змініть локальні значення:

```text
MYSQL_PASSWORD
MYSQL_ROOT_PASSWORD
```

Не додавайте `.env` до Git. Якщо файл уже існує, збережіть його налаштування, не перезаписуючи шаблоном. Паролі MySQL задаються при першій ініціалізації порожнього volume: зміна `.env` сама по собі не змінює пароль у вже створеній БД.

Зберіть і запустіть проєкт рекомендованим способом:

```powershell
docker compose up -d --build
```

Compose запускає `mysql` та `api`, очікує готовності MySQL і передає API рядок підключення із `Server=mysql`. За `APPLY_MIGRATIONS=true` застосунок застосовує необхідні EF Core migrations та додає відсутні початкові дані перед запуском API. Повторний запуск не дублює seed.

- Демонстраційний вебінтерфейс: [http://localhost:8080/](http://localhost:8080/).
- Swagger UI: [http://localhost:8080/swagger](http://localhost:8080/swagger).
- OpenAPI JSON: [http://localhost:8080/openapi/v1.json](http://localhost:8080/openapi/v1.json).
- MySQL Workbench: host `localhost`, port `3307`, база даних та облікові дані з `.env`.

Swagger UI та OpenAPI доступні в середовищі Development, яке налаштоване в поточному Docker Compose. Swagger описує наявні маршрути, DTO, параметри й HTTP-відповіді; розрахунок ціни виконується під час створення бронювання.

API image потребує MySQL, налаштувань середовища та мережі Compose. Не використовуйте `docker run conference-room-booking-api:latest` як основний спосіб запуску. В IDE обирайте конфігурацію Docker Compose для `docker-compose.yml`.

## Перегляд стану та журналів

Очікувані сервіси: `api` — running, `mysql` — running / healthy. Стандартні імена контейнерів: `conference-room-booking-api-1` і `conference-room-booking-mysql-1`.

Перевірити стан контейнерів:

```powershell
docker compose ps
```

Журнал API:

```powershell
docker compose logs -f api
```

Журнал MySQL:

```powershell
docker compose logs -f mysql
```

## Зупинка та повторний запуск

Зупинити й видалити контейнери та мережу, зберігши MySQL volume і дані:

```powershell
docker compose down
```

Перебудувати image та запустити сервіси зі збереженими даними:

```powershell
docker compose up -d --build
```

Compose керує життєвим циклом контейнерів через project/service labels. Повторний запуск цієї команди не потребує створення окремих контейнерів вручну.

## Повне скидання локальної БД

**Увага: `docker compose down -v` видаляє MySQL volume `conference-room-booking_mysql_data` і всі локальні дані БД, включно з бронюваннями. Це не звичайна команда зупинки або очищення контейнерів.**

Виконуйте її лише тоді, коли свідомо хочете почати з порожньої БД:

```powershell
docker compose down -v
docker compose up -d --build
```

Ініціалізація: параметр APPLY_MIGRATIONS=true вмикає застосування EF Core migrations. Окремий параметр Database:SeedOnStartup керує початковим заповненням БД. 
Після застосування migrations ініціалізація БД виконується відповідно до налаштувань застосунку.
