# HardwareRegistry

Тестовое задание на позицию Middle Full-stack разработчика (C# / JS): модуль учёта компьютерной техники.

Сервер на ASP.NET Core (.NET 10) и PostgreSQL. Клиент сделан в двух вариантах над одним API:

- `client-jquery` — jQuery + DevExtreme, как в ТЗ, порт 4300;
- `client` — Angular 22 + `devextreme-angular`, порт 4200.

- Карточка техники: учётный номер (только цифры и латиница, до 32 символов, уникальный), наименование
  до 256 символов, тип из словаря, комната от 1 до 1000.
- Словарь типов лежит в таблице `device_types`. Если он пуст при старте, заполняется пятью типами.
- Лимит 2000 записей. Проверка и вставка идут в одной транзакции под advisory-блокировкой, так что
  параллельные запросы его не обходят.
- Таблица с сортировкой по всем колонкам. Сортировка и пагинация делаются на сервере.
- Создание и редактирование в диалоге: новая запись — кнопкой «Добавить», существующую можно открыть
  кнопкой в строке или двойным кликом.
- Ограничения проверяются на клиенте, в API и в самой БД.
- Таблица обновляется в реальном времени через SignalR (хаб `/hubs/devices`): изменения, сделанные
  в другой вкладке или другим пользователем, видны без перезагрузки страницы. После обрыва связи
  клиент переподключается сам и перечитывает данные.

## Запуск через Aspire

Нужны .NET 10 SDK, Node.js 22+ и Docker.

```powershell
dotnet tool restore
aspire start --non-interactive
```

Aspire поднимает PostgreSQL, применяет миграции, API и оба клиента. Адреса и Swagger смотрите в Aspire Dashboard.

## Запуск без Aspire

1. PostgreSQL: `docker compose up -d`. По умолчанию строка подключения такая:
   `Host=localhost;Port=5432;Database=hardwareregistry;Username=postgres;Password=postgres`.
2. Миграции:

   ```powershell
   dotnet run --project backend/src/HardwareRegistry.DatabaseMigrator
   ```

3. API на http://localhost:5080 (Swagger: http://localhost:5080/swagger):

   ```powershell
   dotnet run --project backend/src/HardwareRegistry.Api --launch-profile http
   ```

4. Клиент, любой или оба сразу. Запросы `/api` уходят на API через прокси.

   ```powershell
   cd client-jquery   # или cd client
   npm ci
   npm start
   ```

## Тесты

Сервер: юнит-тесты по слоям (`backend/tests/*.UnitTests`) и интеграционные (`HardwareRegistry.IntegrationTests`)
на настоящем PostgreSQL. Интеграционные проверяют репозитории, ограничения БД, лимит при параллельных
запросах, HTTP API целиком и сообщения SignalR.

```powershell
dotnet test
```

Для интеграционных тестов нужен запущенный Docker: PostgreSQL поднимается в контейнере сам. Если хотите
использовать свой сервер, укажите строку подключения к нему, и тесты создадут на нём временную базу,
а после прогона удалят её:

```powershell
$env:HARDWARE_REGISTRY_TEST_POSTGRES = "Host=localhost;Username=postgres;Password=postgres"
dotnet test
```

Клиенты тестируются на Vitest в настоящем Chromium: в jsdom диалоги DevExtreme не работают. Перед первым
запуском браузер нужно поставить:

```powershell
cd client          # или cd client-jquery
npx playwright install chromium
npm test
```

## Ошибки

Ответы об ошибках приходят в формате ProblemDetails с полем `code`. Основные:
`Device.AlreadyExists` (409, номер занят), `Operation.LimitExceeded` (400, лимит достигнут),
`Device.NotFound` (404).

## DevExtreme

Ключ лицензии не задан, поэтому виджеты показывают баннер пробной версии. Ключ добавляется через
`config({ licenseKey })` в `client-jquery/src/main.js` или `client/src/main.ts`.
